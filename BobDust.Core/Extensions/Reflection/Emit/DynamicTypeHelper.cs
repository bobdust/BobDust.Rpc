using System.Reflection.Emit;
using System.Reflection;
using System.Dynamic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace BobDust.Core.Extensions.Reflection.Emit
{
   public static class DynamicTypeHelper
   {
      private static ConcurrentDictionary<string, object> _assemblies;
      private static ConcurrentDictionary<string, ModuleBuilder> _modules;
      private static ConcurrentDictionary<string, Type> _types;

      static DynamicTypeHelper()
      {
         _assemblies = new ConcurrentDictionary<string, object>();
         _modules = new ConcurrentDictionary<string, ModuleBuilder>();
         _types = new ConcurrentDictionary<string, Type>();
      }

      private static void DisableDebugOptimization(this AssemblyBuilder assemblyBuilder)
      {
         // Add a debuggable attribute to the assembly saying to disable optimizations
         Type daType = typeof(DebuggableAttribute);
         ConstructorInfo daCtor = daType.GetConstructor([typeof(DebuggableAttribute.DebuggingModes)])!;
         CustomAttributeBuilder daBuilder = new CustomAttributeBuilder(daCtor, [
            DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.Default]);
         assemblyBuilder.SetCustomAttribute(daBuilder);
      }

      private static object DefineDynamicAssembly(this AppDomain domain, string name, bool inMemoryOnly)
      {
         lock (_assemblies)
         {
            if (_assemblies.ContainsKey(name))
            {
               return _assemblies[name];
            }
         }
         var assembly = Assembly.GetCallingAssembly();
         var assemblyName = new AssemblyName { Name = name, Version = assembly.GetName().Version };
         AssemblyBuilder assemblyBuilder;
         if (inMemoryOnly)
         {
            assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
            // assemblyBuilder.DisableDebugOptimization();
            _assemblies[name] = assemblyBuilder;
            return assemblyBuilder;
         }
         assemblyBuilder = new PersistedAssemblyBuilder(assemblyName, typeof(object).Assembly);
         assemblyBuilder.DisableDebugOptimization();
         dynamic wrapper = new ExpandoObject();
         wrapper.Builder = assemblyBuilder;
         var fileName = string.Format("{0}.dll", Path.Combine(Path.GetDirectoryName(assembly.Location)!, name));
         wrapper.FileName = fileName;
         _assemblies[name] = wrapper;
         return wrapper;
      }

      private static ModuleBuilder DefineDynamicModule(object assemblyBuilder)
      {
         AssemblyBuilder builder;
         var moduleName = string.Empty;
         ModuleBuilder module;
         if (assemblyBuilder is ExpandoObject)
         {
            dynamic wrapper = assemblyBuilder;
            var fileName = wrapper.FileName;
            builder = (wrapper.Builder as AssemblyBuilder)!;
            moduleName = builder.GetName().Name!;
            lock (_modules)
            {
               if (_modules.ContainsKey(moduleName))
               {
                  return _modules[moduleName];
               }
            }
            module = builder.DefineDynamicModule(moduleName);
            _modules[moduleName] = module;
            return module;
         }
         builder = (assemblyBuilder as AssemblyBuilder)!;
         moduleName = builder.GetName().Name!;
         lock (_modules)
         {
            if (_modules.ContainsKey(moduleName))
            {
               return _modules[moduleName];
            }
         }
         module = builder.DefineDynamicModule(moduleName);
         _modules[moduleName] = module;
         return module;
      }

      private static Type CreateDynamicType(Type baseType, Func<string> getTypeName, Action<TypeBuilder> build)
      {
         var assembly = Assembly.GetEntryAssembly();
         if (assembly == null)
         {
            assembly = Assembly.GetCallingAssembly();
         }
         var typeNamespace = assembly.GetName().Name!;
         return CreateDynamicType(baseType, typeNamespace, getTypeName, build);
      }

      private static Type CreateDynamicType(
         Type baseType,
         string typeNamespace,
         Func<string> getTypeName, Action<TypeBuilder> build,
         bool persisted = false)
      {
         var name = $"{typeNamespace}.Dynamic{(persisted ? $".{getTypeName()}" : string.Empty)}";
         AssemblyBuilder assemblyBuilder;
         var fileName = string.Empty;
         var obj = AppDomain.CurrentDomain.DefineDynamicAssembly(name, !persisted);
         if (obj is ExpandoObject)
         {
            dynamic objDynamic = obj;
            assemblyBuilder = objDynamic.Builder;
            fileName = objDynamic.FileName;
         }
         else
         {
            assemblyBuilder = (obj as AssemblyBuilder)!;
         }
         var module = DefineDynamicModule(obj);
         var typeName = persisted ? name : $"{name}.{getTypeName()}";
         lock (_types)
         {
            if (_types.ContainsKey(typeName))
            {
               return _types[typeName];
            }
         }
         var typeBuilder = module.DefineType(typeName, TypeAttributes.NotPublic, baseType);
         typeBuilder.CreatePassThroughConstuctors(baseType);
         build(typeBuilder);
         var type = typeBuilder.CreateType();
         if (!string.IsNullOrEmpty(fileName) && assemblyBuilder is PersistedAssemblyBuilder pab)
         {
            pab.Save(fileName);
            var persistedAssembly = Assembly.LoadFile(fileName);
            var persistedType = persistedAssembly.GetType(type.FullName!)!;
            _types[typeName] = persistedType;
            return persistedType;
         }
         else
         {
            _types[typeName] = type;
            return type;
         }
      }

      private static Type CreateDynamicType(Type baseType, Type contractType, Action<MethodInfo, ILGenerator> emit)
      {
         var type = CreateDynamicType(
            baseType,
            contractType.Namespace!,
            () =>
            {
               return contractType.Name.TrimStart('I');
            },
            (typeBuilder) =>
            {
               typeBuilder.AddInterfaceImplementation(contractType);
               foreach (var method in contractType.GetMethods())
               {
                  var parameters = method.GetParameters();
                  var methodBuilder = typeBuilder.DefineMethod(
                     string.Format("{0}.{1}", contractType.Name, method.Name),
                     MethodAttributes.Private | MethodAttributes.HideBySig |
                     MethodAttributes.NewSlot | MethodAttributes.Virtual |
                     MethodAttributes.Final,
                     method.ReturnType,
                     parameters.Select(p => p.ParameterType).ToArray());
                  for (var i = 0; i < parameters.Length; i++)
                  {
                     methodBuilder.DefineParameter(i + 1, ParameterAttributes.None, parameters[i].Name);
                  }
                  var emitter = methodBuilder.GetILGenerator();
                  emit(method, emitter);
                  typeBuilder.DefineMethodOverride(methodBuilder, method);
               }
            });
         return type;
      }

      private static Type Implement(this Type baseType, Type contractType, Action<MethodInfo, ILGenerator> emit)
      {
         return CreateDynamicType(baseType, contractType, emit);
      }

      public static bool IsAsync(this MethodInfo method)
      {
         return method.ReturnType == typeof(Task) || (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
      }

      public static Type Implement(
         this Type baseType, 
         Type contractType, 
         Func<MethodInfo, MethodInfo> getBaseMethod,
         Action<ILGenerator, Type, MethodInfo, MethodInfo> emitSyncBody,
         Action<ILGenerator, Type, MethodInfo, MethodInfo> emitAsyncBody)
      {
         var type = baseType.Implement(contractType, (method, emitter) =>
         {
            var invoke = getBaseMethod(method);
            if (method.IsAsync())
            {
               emitAsyncBody(emitter, contractType, method, invoke);
            }
            else
            {
               emitSyncBody(emitter, contractType, method, invoke);
            }
         });
         return type;
      }

      public static Type Extend(this Type baseType, string typeName)
      {
         var type = CreateDynamicType(
            baseType,
            () =>
            {
               return typeName.TrimStart('I');
            },
            (typeBuilder) =>
            {
            });
         return type;
      }

      /// <summary>Creates one constructor for each public constructor in the base class. Each constructor simply
      /// forwards its arguments to the base constructor, and matches the base constructor's signature.
      /// Supports optional values, and custom attributes on constructors and parameters.
      /// Does not support n-ary (variadic) constructors</summary>
      public static void CreatePassThroughConstuctors(this TypeBuilder builder, Type baseType)
      {
         foreach (var constructor in baseType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic))
         {
            var parameters = constructor.GetParameters();

            //*
            if (parameters.Length > 0 && parameters.Last().IsDefined(typeof(ParamArrayAttribute), false))
            {
               //throw new InvalidOperationException("Variadic constructors are not supported");
               continue;
            }
            //*/

            var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();
            var requiredCustomModifiers = parameters.Select(p => p.GetRequiredCustomModifiers()).ToArray();
            var optionalCustomModifiers = parameters.Select(p => p.GetOptionalCustomModifiers()).ToArray();

            var ctor = builder.DefineConstructor(MethodAttributes.Public, constructor.CallingConvention, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
            for (var i = 0; i < parameters.Length; ++i)
            {
               var parameter = parameters[i];
               var parameterBuilder = ctor.DefineParameter(i + 1, parameter.Attributes, parameter.Name);
               if (((int)parameter.Attributes & (int)ParameterAttributes.HasDefault) != 0)
               {
                  parameterBuilder.SetConstant(parameter.RawDefaultValue);
               }

               foreach (var attribute in BuildCustomAttributes(parameter.GetCustomAttributesData()))
               {
                  parameterBuilder.SetCustomAttribute(attribute);
               }
            }

            foreach (var attribute in BuildCustomAttributes(constructor.GetCustomAttributesData()))
            {
               ctor.SetCustomAttribute(attribute);
            }

            var emitter = ctor.GetILGenerator();
            emitter.Emit(OpCodes.Nop);

            // Load `this` and call base constructor with arguments
            emitter.Emit(OpCodes.Ldarg_0);
            for (var i = 1; i <= parameters.Length; ++i)
            {
               emitter.Emit(OpCodes.Ldarg, i);
            }
            emitter.Emit(OpCodes.Call, constructor);

            emitter.Emit(OpCodes.Ret);
         }
      }

      private static CustomAttributeBuilder[] BuildCustomAttributes(IEnumerable<CustomAttributeData> customAttributes)
      {
         return customAttributes.Select(attribute =>
         {
            var attributeArgs = attribute.ConstructorArguments.Select(a => a.Value is ReadOnlyCollection<CustomAttributeTypedArgument> customAttributeTypedArguments ? customAttributeTypedArguments.Select(arg => arg.Value).ToArray() : a.Value).ToArray();
            var namedPropertyInfos = attribute.NamedArguments.Select(a => a.MemberInfo).OfType<PropertyInfo>().ToArray();
            var namedPropertyValues = attribute.NamedArguments.Where(a => a.MemberInfo is PropertyInfo).Select(a => a.TypedValue.Value).ToArray();
            var namedFieldInfos = attribute.NamedArguments.Select(a => a.MemberInfo).OfType<FieldInfo>().ToArray();
            var namedFieldValues = attribute.NamedArguments.Where(a => a.MemberInfo is FieldInfo).Select(a => a.TypedValue.Value).ToArray();
            return new CustomAttributeBuilder(attribute.Constructor, attributeArgs, namedPropertyInfos, namedPropertyValues, namedFieldInfos, namedFieldValues);
         }).ToArray();
      }
   }
}
