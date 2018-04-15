using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sb49.Common.Droid
{
    /// <summary>
    /// Класс-helper чтения методанных сборок.
    /// </summary>
    public static class AssemblyAttributeAccessors
    {
        public static string GetAssemblyTitle(Type type)
        {
            // Get all Title attributes on this assembly
            var assembly = GetAssembly(type);
            return GetAssemblyTitle(assembly);
        }

        public static string GetAssemblyFileVersion(Type type)
        {
            var assembly = GetAssembly(type);
            var result = GetAssemblyFileVersion(assembly);
            return string.IsNullOrEmpty(result) ? "0.0.0.0" : result;
        }

        public static Version GetAssemblyVersion(Type type)
        {
            return GetAssembly(type)?.GetName().Version;
        }

        public static string AssemblyDescription(Type type)
        {
            var assembly = GetAssembly(type);
            var attributes = GetCustomAttributes(assembly, typeof(AssemblyDescriptionAttribute), false);
            if (attributes.Length == 0)
                return string.Empty;
            return ((AssemblyDescriptionAttribute) attributes[0]).Description;
        }

        public static string GetAssemblyProduct(Type type)
        {
            var assembly = GetAssembly(type);
            var attributes = GetCustomAttributes(assembly, typeof(AssemblyProductAttribute), false);
            if (attributes.Length == 0)
                return string.Empty;
            return ((AssemblyProductAttribute) attributes[0]).Product;
        }

        public static string GetAssemblyCopyright(Type type)
        {
            var assembly = GetAssembly(type);
            var attributes = GetCustomAttributes(assembly, typeof(AssemblyCopyrightAttribute), false);
            if (attributes.Length == 0)
                return string.Empty;
            return ((AssemblyCopyrightAttribute) attributes[0]).Copyright;
        }

        public static string GetAssemblyCompany(Type type)
        {
            var assembly = GetAssembly(type);
            var attributes = GetCustomAttributes(assembly, typeof(AssemblyCompanyAttribute), false);
            if (attributes.Length == 0)
                return string.Empty;
            return ((AssemblyCompanyAttribute) attributes[0]).Company;
        }

        public static string GetAssemblyFileVersion(Assembly assembly)
        {
            if (assembly == null)
                return string.Empty;

            var attributes = GetCustomAttributes(assembly, typeof(AssemblyFileVersionAttribute), false);

            return attributes.Length == 0
                ? string.Empty
                : ((AssemblyFileVersionAttribute) attributes[0]).Version;
        }

        public static string GetAssemblyInformationalVersion(Assembly assembly, string separator = ";")
        {
            if (assembly == null)
                return string.Empty;

            string result = null;
            var attributes = GetCustomAttributes(assembly, typeof(AssemblyInformationalVersionAttribute), false);
            if (attributes.Length > 0)
            {
                var verinfo = ((AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;
                if (!string.IsNullOrEmpty(verinfo))
                {
                    var vers = verinfo.Split(separator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    result = vers.Length > 0 ? vers[0] : verinfo;
                }
            }

            if (!string.IsNullOrEmpty(result))
                return result;

            result = GetAssemblyFileVersion(assembly);
            if (!string.IsNullOrEmpty(result))
                return result;

            return assembly.GetName().Version.ToString();
        }

        public static string GetAssemblyTitle(Assembly assembly)
        {
            if (assembly == null)
                return string.Empty;

            var attributes = GetCustomAttributes(assembly, typeof(AssemblyTitleAttribute), false);
            if (attributes.Length > 0)
            {
                // Select the first one
                var titleAttribute = (AssemblyTitleAttribute) attributes[0];
                // If it is not an empty string, return it
                if (!string.IsNullOrEmpty(titleAttribute.Title))
                    return titleAttribute.Title;
            }
            // If there was no Title attribute, or if the Title attribute was the empty string, return the .exe name
            return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().CodeBase);
        }

        public static Assembly[] FindAssemblies(Func<Assembly, bool> filterHandler)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var result = new List<Assembly>();

            if (filterHandler == null)
            {
                result.AddRange(assemblies);
            }
            else
            {
                var filterresult = assemblies.Where(filterHandler).ToArray();
                if (filterresult.Length > 0)
                    result.AddRange(filterresult);
            }
            return result.ToArray();
        }

        private static Assembly GetAssembly(Type type)
        {
            if (type == null)
            {
                //var assembly = Assembly.GetEntryAssembly();
                var assembly = Assembly.GetExecutingAssembly();
                //var assembly = Assembly.GetCallingAssembly();
                return assembly;
            }

            return Assembly.GetAssembly(type);
        }

        private static object[] GetCustomAttributes(Assembly assembly, Type attributeType, bool inherit)
        {
            if(assembly == null)
                return new object[0];

            var attributes  = assembly.GetCustomAttributes(attributeType, inherit);
            return attributes;
        }
    }
}
