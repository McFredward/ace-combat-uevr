﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AceCombat7_dotnet8.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("AceCombat7_dotnet8.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///    &quot;GameName&quot;: &quot;Ace Combat 7&quot;,
        ///    &quot;Name&quot;: &quot;Default&quot;,
        ///    &quot;components&quot;: [
        ///        {
        ///            &quot;Constant&quot;: false,
        ///            &quot;Input_index&quot;: 0,
        ///            &quot;Output_index&quot;: 0,
        ///            &quot;MultiplierPos&quot;: 1.0,
        ///            &quot;MultiplierNeg&quot;: 1.0,
        ///            &quot;Offset&quot;: 0.0,
        ///            &quot;Inverse&quot;: false,
        ///            &quot;Limit&quot;: -1.0,
        ///            &quot;Smoothing&quot;: 1.0,
        ///            &quot;Enabled&quot;: true,
        ///            &quot;Spikeflatter&quot;: {
        ///                &quot;Enabled&quot;: false,
        ///                &quot;Limit&quot;: 100.0,
        ///         [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string defaultProfile {
            get {
                return ResourceManager.GetString("defaultProfile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h1&gt;UEVR Installation Guide&lt;/h1&gt;
        ///				&lt;h2&gt;Installation Steps&lt;/h2&gt;
        ///				&lt;ol&gt;
        ///					&lt;li&gt;Download &lt;a href=&quot;https://github.com/praydog/UEVR-nightly/releases&quot; target=&quot;_blank&quot;&gt;the latest UEVR Nightly version&lt;/a&gt; and extract it to any location.&lt;/li&gt;
        ///					&lt;li&gt;Download UESS from &lt;a href=&quot;https://www.nexusmods.com/acecombat7skiesunknown/mods/2474?tab=files&quot; target=&quot;_blank&quot;&gt;this link&lt;/a&gt; and extract the files into the game folder located at &quot;...\common\ACE COMBAT 7&quot;.&lt;/li&gt;
        ///					&lt;li&gt;Create the following directories:
        /// [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string desc {
            get {
                return ResourceManager.GetString("desc", resourceCulture);
            }
        }
    }
}