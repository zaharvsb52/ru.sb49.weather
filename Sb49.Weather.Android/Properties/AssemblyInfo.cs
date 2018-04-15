using System.Reflection;
using Android.App;

// Information about this assembly is defined by the following attributes.
// Change them to the values specific to your project.

[assembly: AssemblyTitle("Sb49.Weather")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyProduct("Sb49.Weather")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyCompany("SB49 Software")]
[assembly: AssemblyCopyright("Copyright SB49 Software 2012-2018")]
[assembly: AssemblyTrademark("SB49 Software")]

// The assembly version has the format "{Major}.{Minor}.{Build}.{Revision}".
// The form "{Major}.{Minor}.*" will automatically update the build and revision,
// and "{Major}.{Minor}.{Build}.*" will update just the revision.

[assembly: AssemblyVersion("1.0.7.1")]
[assembly: AssemblyFileVersion("1.0.7.1")]

// The following attributes are used to specify the signing key for the assembly,
// if desired. See the Mono documentation for more information about signing.

//[assembly: AssemblyDelaySign(false)]
//[assembly: AssemblyKeyFile("")]

//Custom permission
[assembly: UsesPermission("com.google.android.c2dm.permission.RECEIVE")]