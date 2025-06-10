
using System;
using Microsoft.MetadirectoryServices;
using System.Xml;

namespace Mms_Metaverse
{
	/// <summary>
	/// Summary description for MVExtensionObject.
	/// </summary>
    public class MVExtensionObject : IMVSynchronization
    {

        //XML Variables
        string Whitepages_MAName;
        public MVExtensionObject()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        void IMVSynchronization.Initialize ()
        {

            //Open configuration XML file in the Extensions directory
            const string XML_CONFIG_FILE = @"\MIMConfig.xml";
            XmlDocument config = new XmlDocument();
            string dir = Utils.ExtensionsDirectory;
            config.Load(dir + XML_CONFIG_FILE);

            XmlNode rnode = config.SelectSingleNode("rules-extension-properties/management-agents");

            //Read the MA name that is used for Whitepages
            XmlNode node = rnode.SelectSingleNode("Whitepages/MAName");
            Whitepages_MAName = node.InnerText;

            //
            // TODO: Add initialization logic here
            //
        }

        void IMVSynchronization.Terminate ()
        {
            //
            // TODO: Add termination logic here
            //
        }

        void IMVSynchronization.Provision (MVEntry mventry)
        {

            //The code below will disconnect a Whitepages object if the employeeID is blank.  The only way to get into this situation is if we once joined on employeeID and it has been cleared
            ConnectedMA WhitepagesMA = mventry.ConnectedMAs[Whitepages_MAName];
            if (WhitepagesMA.Connectors.Count > 0 && !mventry["employeeID"].IsPresent)
            {
                WhitepagesMA.Connectors.DeprovisionAll();
            }

			//
			// TODO: Remove this throw statement if you implement this method
			//
			//throw new EntryPointNotImplementedException();
        }	

        bool IMVSynchronization.ShouldDeleteFromMV (CSEntry csentry, MVEntry mventry)
        {
            //
            // TODO: Add MV deletion logic here
            //
            throw new EntryPointNotImplementedException();
        }
    }
}
