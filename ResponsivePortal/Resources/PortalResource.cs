using KBCommon.KBException;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace ResponsivePortal.Resources
{
   
    public class PortalResource
    {
        private string _configRootPath;
        private Dictionary<string, Dictionary<string, string>> Resources;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public PortalResource(string configRootPath)
        {
            if (string.IsNullOrWhiteSpace(configRootPath)) throw new ArgumentException("ConfigRootPath Null/Empty/WhiteSpaces");
            _configRootPath = configRootPath;
            Resources = new Dictionary<string, Dictionary<string, string>>();
        }

        private void LoadResource(string configRootPath)
        {
            if (string.IsNullOrWhiteSpace(configRootPath)) throw new ArgumentException("ConfigRootPath Null/Empty/WhiteSpaces");
            _configRootPath = configRootPath;

        }

        public KBResource LoadConfiguration(int clientId, int portalId, string language, bool reload)
        {
            string fileName = null;
            string path = null;
            try
            {
                fileName = Utilities.GetResourceFileName(language);
                path = GetXmlConfigPath(clientId, portalId, fileName);
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException("Resource file does not exist", path);
                }
                var xdoc = XDocument.Load(path);
                ReadAndFillResource(xdoc);
            }
            catch (FileNotFoundException ex)
            {
                string userError = string.Format("{0}: {1}", GeneralResources.IOError, ex.Message);
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.LoadConfigFile, KBErrorHandler.GetMethodName(), userError,
                    new KBExceptionData("clientId", clientId), new KBExceptionData("portalId", portalId), new KBExceptionData("path", path), new KBExceptionData("fileName", fileName));
                throw kbCustExp;
            }
            KBResource res = new KBResource(portalId,clientId);
            res.Resources = Resources;
            return res;
        }

        public string Resource(string modulename, string resourcekey)
        {
            try
            {
                return Resources[modulename][resourcekey];
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private string GetXmlConfigPath(int clientId, int portalId, string fileName)
        {
            return Path.Combine(_configRootPath, "knowledgebase", "PortalConfiguration", clientId.ToString(), portalId.ToString(), "Languages", fileName);           
        }

        private void ReadAndFillResource(XDocument xdoc)
        {
            Resources = new Dictionary<string, Dictionary<string, string>>();
            //Modules
            var resourceModules = xdoc.Elements("portal").Elements();           
            foreach (XElement module in resourceModules)
            {
                Dictionary<string, string>  ResDict = new Dictionary<string, string>();
                foreach (XElement element in module.Elements())
                {
                    if (!ResDict.ContainsKey(element.Name.ToString().ToUpper()))
                    {
                        ResDict.Add(element.Name.ToString().ToUpper(), element.Value);
                    }
                }
                if (!Resources.ContainsKey(module.Name.ToString().ToUpper()))
                {
                    Resources.Add(module.Name.ToString().ToUpper(), ResDict);
                }               
            }
        }
    }

    public class PortalResourceManager
    {
        public PortalResourceManager(PortalResource portalResourceRepository)
        {
            if (portalResourceRepository == null) throw new ArgumentNullException("Portal Resource is null");
            this.PortalResourceConfigRepository = portalResourceRepository;
        }

        private PortalResource PortalResourceConfigRepository { get; set; }
        public KBResource LoadConfiguration(int clientId, int portalId, string language, bool reload = false)
        {
            return this.PortalResourceConfigRepository.LoadConfiguration(clientId, portalId, language, reload);
            
        }
    }
}