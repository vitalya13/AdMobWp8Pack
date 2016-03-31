#if UNITY_EDITOR && UNITY_WP8

using System.IO;
using System.Xml.Linq;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AdMobWp8
{
    public class Generate
    {
        [PostProcessBuild]
        static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            string productName = PlayerSettings.productName;
            string projectPath = string.Format("{0}/{1}/{1}.csproj", pathToBuiltProject, productName);
            XDocument doc = XDocument.Load(projectPath);

            // add POST_BUILD directive
            foreach (XElement el in doc.Root.Elements())
            {
                if (el.Name.LocalName != "PropertyGroup")
                {
                    continue;
                }

                foreach (XElement el1 in el.Elements())
                {
                    if (el1.Name.LocalName == "DefineConstants")
                    {
                        bool isDirective = false;

                        foreach (string directive in el1.Value.Split(';', ' '))
                        {
                            if (directive == "POST_BUILD")
                            {
                                isDirective = true;
                                break;
                            }
                        }

                        if (!isDirective)
                        {
                            el1.Value += ";POST_BUILD";
                        }
                    }
                }
            }

            // add cs files to wp8 project
            string[] files = {
                string.Format("{0}/AdMobWp8/Plugins/AdMobWp8Plugin.cs", Application.dataPath)
            };
            addFilesToProject(files, string.Format("{0}/{1}", pathToBuiltProject, productName), "AdMobWp8", doc);
            doc.Save(projectPath);

            // add manifest data
            string manifestPath = string.Format("{0}/{1}/Properties/WMAppManifest.xml", pathToBuiltProject, productName);
            doc = XDocument.Load(manifestPath);
            IEnumerable<XElement> capabilityItems = from app in doc.Root.Elements()
                                                    from capabilities in app.Elements()
                                                    from capability in capabilities.Elements()
                                                    from capAttr in capability.Attributes()
                                                    where
                                                        app.Name.LocalName == "App" &&
                                                        capabilities.Name.LocalName == "Capabilities" &&
                                                        capability.Name.LocalName == "Capability" &&
                                                        capAttr.Name.LocalName == "Name" &&
                                                        capAttr.Value == "ID_CAP_WEBBROWSERCOMPONENT"
                                                    select capability;

            if (capabilityItems.Count() == 0)
            {
                XElement el2 = new XElement(XName.Get("Capability"));
                el2.SetAttributeValue(XName.Get("Name"), "ID_CAP_WEBBROWSERCOMPONENT");
                capabilityItems = from app in doc.Root.Elements()
                                  from capabilities in app.Elements()
                                  where
                                    app.Name.LocalName == "App" &&
                                    capabilities.Name.LocalName == "Capabilities"
                                  select capabilities;
                capabilityItems.First().Add(el2);
                doc.Save(manifestPath);
            }

            // add AdMobWp8 initialize in MainPage.xaml.cs file
            string mainPagePath = string.Format("{0}/{1}/MainPage.xaml.cs", pathToBuiltProject, productName);
            string mainPage = File.ReadAllText(mainPagePath);

            if (mainPage.IndexOf("AdMobWp8.Creator.init(DrawingSurfaceBackground, this.Dispatcher);") == -1)
            {
                Match match = Regex.Match(mainPage, @"public MainPage\(\)\s*{(?'constructorCode'.+?)}", RegexOptions.Singleline);
                string newConstructor = string.Format("{0}\tAdMobWp8.Creator.init(DrawingSurfaceBackground, this.Dispatcher);\n\t\t", match.Groups["constructorCode"]);
                File.WriteAllText(mainPagePath, mainPage.Replace(match.Groups["constructorCode"].ToString(), newConstructor));
            }
        }

        static void addFilesToProject(string[] files, string destinationPath, string destinationFolder, XDocument doc)
        {
            foreach (string sourcePath in files)
            {
                string fileName = Path.GetFileName(sourcePath);
                destinationPath = string.Format("{0}/{1}", destinationPath, destinationFolder);

                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }

                File.Copy(sourcePath, string.Format("{0}/{1}", destinationPath, fileName), true);

                // creating attribute in project file
                string attrIncludeValue = string.Format(@"{0}\{1}", destinationFolder, fileName);
                IEnumerable<XElement> compileElements = from itemGroup in doc.Root.Elements()
                                                        from compile in itemGroup.Elements()
                                                        from compileAttr in compile.Attributes()
                                                        where
                                                            itemGroup.Name.LocalName == "ItemGroup" &&
                                                            compile.Name.LocalName == "Compile" &&
                                                            compileAttr.Name.LocalName == "Include" &&
                                                            compileAttr.Value == attrIncludeValue
                                                        select compile;

                if (compileElements.Count() == 0)
                {
                    XElement el2 = new XElement(XName.Get("Compile", doc.Root.GetDefaultNamespace().NamespaceName));
                    el2.SetAttributeValue(XName.Get("Include"), attrIncludeValue);
                    IEnumerable<XElement> itemGroups = from itemGroup in doc.Root.Elements()
                                                       from compile in itemGroup.Elements()
                                                       where
                                                            itemGroup.Name.LocalName == "ItemGroup" &&
                                                            compile.Name.LocalName == "Compile"
                                                       select itemGroup;
                    itemGroups.First().Add(el2);
                }
            }
        }
    }
}

#endif

