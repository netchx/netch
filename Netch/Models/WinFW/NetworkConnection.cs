using System;
using NETCONLib;

namespace Netch.Models.WinFW
{
    public class NetworkConnection : INetConnection, INetConnectionProps, INetSharingConfiguration
    {
        private readonly INetConnection _icsConn;

        private readonly NetSharingManager _icsMgr;

        public NetworkConnection(INetConnection icsConnection)
        {
            _icsMgr = new NetSharingManagerClass();
            _icsConn = icsConnection;
        }

        public void Connect()
        {
            _icsConn.Connect();
        }

        public void Delete()
        {
            _icsConn.Delete();
        }

        public void Duplicate(string pszwDuplicateName, out INetConnection ppCon)
        {
            _icsConn.Duplicate(pszwDuplicateName, out ppCon);
        }

        public void Disconnect()
        {
            _icsConn.Disconnect();
        }

        public void GetProperties(IntPtr ppProps)
        {
            _icsConn.GetProperties(ppProps);
        }

        public void Rename(string pszwNewName)
        {
            _icsConn.Rename(pszwNewName);
        }

        public void GetUiObjectClassId(out Guid pclsid)
        {
            _icsConn.GetUiObjectClassId(out pclsid);
        }

        public uint Characteristics => _icsMgr.NetConnectionProps[_icsConn].Characteristics;

        public string DeviceName => _icsMgr.NetConnectionProps[_icsConn].DeviceName;

        public string Guid => _icsMgr.NetConnectionProps[_icsConn].Guid;

        public tagNETCON_MEDIATYPE MediaType => _icsMgr.NetConnectionProps[_icsConn].MediaType;

        public string Name => _icsMgr.NetConnectionProps[_icsConn].Name;

        public tagNETCON_STATUS Status => _icsMgr.NetConnectionProps[_icsConn].Status;

        public INetSharingPortMapping AddPortMapping(string bstrName, byte ucIPProtocol, ushort usExternalPort,
            ushort usInternalPort, uint dwOptions, string bstrTargetNameOrIPAddress, tagICS_TARGETTYPE eTargetType)
        {
            return _icsMgr.INetSharingConfigurationForINetConnection[_icsConn].AddPortMapping(bstrName,
                ucIPProtocol, usExternalPort, usInternalPort, dwOptions, bstrTargetNameOrIPAddress, eTargetType);
        }

        public void DisableInternetFirewall() => _icsMgr.INetSharingConfigurationForINetConnection[_icsConn].DisableInternetFirewall();

        public void DisableSharing()
        {
            _icsMgr.INetSharingConfigurationForINetConnection[_icsConn].DisableSharing();
        }

        public void EnableInternetFirewall()
        {
            _icsMgr.INetSharingConfigurationForINetConnection[_icsConn].EnableInternetFirewall();
        }

        public void EnableSharing(tagSHARINGCONNECTIONTYPE Type)
        {
            _icsMgr.INetSharingConfigurationForINetConnection[_icsConn].EnableSharing(Type);
        }

        public void RemovePortMapping(INetSharingPortMapping pMapping)
        {
            _icsMgr.INetSharingConfigurationForINetConnection[_icsConn].RemovePortMapping(pMapping);
        }

        public bool InternetFirewallEnabled =>
            _icsMgr.INetSharingConfigurationForINetConnection[_icsConn]
                .InternetFirewallEnabled;

        public INetSharingPortMappingCollection get_EnumPortMappings(tagSHARINGCONNECTION_ENUM_FLAGS Flags)
        {
            return _icsMgr.INetSharingConfigurationForINetConnection[_icsConn]
                .EnumPortMappings[Flags];
        }

        public tagSHARINGCONNECTIONTYPE SharingConnectionType =>
            _icsMgr.INetSharingConfigurationForINetConnection[_icsConn].SharingConnectionType;

        public bool SharingEnabled => _icsMgr.INetSharingConfigurationForINetConnection[_icsConn].SharingEnabled;
        
        public INetSharingConfiguration NetSharingConfigurationForINetConnection()
        {
            return _icsMgr.INetSharingConfigurationForINetConnection[_icsConn];
        }
    }
}