using System;
using System.Reflection;

namespace Airport {
   [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
   sealed class InitializeOnLoadAttribute : Attribute {
   }


   public class Initializer {
      public static void Initialize() {
         var Assembly = typeof(Initializer).Assembly;
         foreach (var Type in Assembly.GetTypes()) {
            foreach (var Method in Type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
               if (Method.IsDefined(typeof(InitializeOnLoadAttribute))) {
                  Method.Invoke(null, null);
               }
            }
         }
      }
   }
}
 