﻿using System;
using System.Collections.Generic;
using System.Linq;
using Cassette.HtmlTemplates;
using Cassette.Scripts;
using Cassette.Stylesheets;
using Cassette.UI;

namespace Cassette
{
    public abstract class CassetteApplicationBase : ICassetteApplication
    {
        public CassetteApplicationBase(ICassetteConfiguration config, IFileSystem sourceFileSystem, IFileSystem cacheFileSystem, bool isOutputOptimized, string version)
        {
            this.sourceFileSystem = sourceFileSystem;
            IsOutputOptimized = isOutputOptimized;
            version = CombineVersionWithCassetteVersion(version);

            this.moduleFactories = new Dictionary<Type, object>
            {
                { typeof(ScriptModule), new ScriptModuleFactory() },
                { typeof(StylesheetModule), new StylesheetModuleFactory() },
                { typeof(HtmlTemplateModule), new HtmlTemplateModuleFactory() }
            };
            var moduleConfiguration = new ModuleConfiguration(this, cacheFileSystem, moduleFactories);
            config.Configure(moduleConfiguration);
            this.moduleContainers = moduleConfiguration.CreateModuleContainers(isOutputOptimized, version);        
        }

        readonly IFileSystem sourceFileSystem;
        readonly List<Action> initializers = new List<Action>();
        readonly Dictionary<Type, IModuleContainer> moduleContainers;
        readonly Dictionary<Type, object> moduleFactories;
        
        public bool IsOutputOptimized { get; private set; }

        public IFileSystem RootDirectory
        {
            get { return sourceFileSystem; }
        }

        public IReferenceBuilder<T> CreateReferenceBuilder<T>()
            where T : Module
        {
            return new ReferenceBuilder<T>(GetModuleContainer<T>(), (IModuleFactory<T>)moduleFactories[typeof(T)]);
        }

        protected IModuleContainer<T> GetModuleContainer<T>()
            where T: Module
        {
            IModuleContainer container;
            if (moduleContainers.TryGetValue(typeof(T), out container))
            {
                return (IModuleContainer<T>)container;
            }
            else
            {
                return new ModuleContainer<T>(Enumerable.Empty<T>());
            }
        }

        public IAsset FindAssetByPath(string path)
        {
            foreach (var container in moduleContainers.Values)
            {
                var asset = container.FindAssetByPath(path);
                if (asset != null) return asset;
            }
            return null;
        }


        public abstract string CreateAbsoluteUrl(string path);
        public abstract string CreateModuleUrl(Module module);
        public abstract string CreateAssetUrl(Module module, IAsset asset);
        public abstract IPageAssetManager<T> GetPageAssetManager<T>() where T : Module;

        /// <remarks>
        /// We need module container cache to depend on both the application version
        /// and the Cassette version. So if either is upgraded, then the cache is discarded.
        /// </remarks>
        string CombineVersionWithCassetteVersion(string version)
        {
            return version + "|" + GetType().Assembly.GetName().Version.ToString();
        }
    }
}