using System;
using System.Reflection;
using Pixagen.Core.App;
using Pixagen.Core.Input;
using Pixagen.Core.Performance;
using Pixagen.Core.Runtime;
using Pixagen.Core.Timing;

namespace Pixagen.Ecs.DI {
    public static class SystemsExtensions {
        public static ISystems Inject(this ISystems systems, params object[] injects) {
            injects = ResolveInjects(systems, injects);
            BindContainerServices(systems, injects);

            InjectGroupServices(systems, injects);

            foreach (var system in systems.AllSystems) {
                InjectToObject(systems, system, injects);
                AfterInject(system);
            }
            
            return systems;
        }

        public static T InjectObject<T>(this ISystems systems, T target, params object[] injects) where T : class {
            if (target is null) {
                throw new ArgumentNullException(nameof(target));
            }

            injects = ResolveInjects(systems, injects);
            BindContainerServices(systems, injects);
            InjectToObject(systems, target, injects);
            AfterInject(target);
            return target;
        }

        private static object[] ResolveInjects(ISystems systems, object[] injects) {
            injects ??= Array.Empty<object>();
            if (systems is not Systems concreteSystems || concreteSystems.GroupInjects.Count == 0) {
                return injects;
            }

            var resolved = new object[injects.Length + concreteSystems.GroupInjects.Count];
            Array.Copy(injects, resolved, injects.Length);
            for (int i = 0; i < concreteSystems.GroupInjects.Count; i++) {
                resolved[injects.Length + i] = concreteSystems.GroupInjects[i];
            }

            return resolved;
        }

        private static void BindContainerServices(ISystems systems, object[] injects) {
            if (systems is not Systems concreteSystems) {
                return;
            }

            for (int i = 0; i < injects.Length; i++) {
                if (injects[i] is Time time) {
                    concreteSystems.SetTime(time);
                    return;
                }
            }
        }

        private static void InjectGroupServices(ISystems systems, object[] injects) {
            if (systems is not Systems concreteSystems) {
                return;
            }

            for (int i = 0; i < concreteSystems.GroupInjects.Count; i++) {
                object inject = concreteSystems.GroupInjects[i];
                if (inject is not null) {
                    InjectToObject(systems, inject, injects);
                }
            }

            for (int i = 0; i < concreteSystems.GroupInjects.Count; i++) {
                object inject = concreteSystems.GroupInjects[i];
                if (inject is not null) {
                    AfterInject(inject);
                }
            }
        }

        private static void InjectToObject(ISystems systems, object target, params object[] injects) {
            foreach (var f in target.GetType ().GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                if (f.IsStatic) {
                    continue;
                }
                
                if (InjectBuiltIns(f, systems, target)) {
                    continue;
                }

                if (InjectCustoms(f, target, injects)) {
                    continue;
                }
            }
        }
        
        private static bool InjectBuiltIns (FieldInfo fieldInfo, ISystems systems, object target) {
            if (typeof (IDataInject).IsAssignableFrom (fieldInfo.FieldType)) {
                var instance = (IDataInject)fieldInfo.GetValue(target)!;
                instance.Fill(systems);
                fieldInfo.SetValue(target, instance);
                return true;
            }
            return false;
        }

        private static bool InjectCustoms(FieldInfo fieldInfo, object target, params object[] injects) {
            if (typeof (ICustomDataInject).IsAssignableFrom (fieldInfo.FieldType)) {
                var instance = (ICustomDataInject)fieldInfo.GetValue(target)!;
                instance.Fill(injects);
                fieldInfo.SetValue(target, instance);
                return true;
            }

            return false;
        }

        private static void AfterInject(object target) {
            if (target is IAfterInject afterInject) {
                afterInject.AfterInject();
            }
        }
    }   
}
