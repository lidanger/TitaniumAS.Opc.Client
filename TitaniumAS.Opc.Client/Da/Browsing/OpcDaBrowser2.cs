﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Common.Logging;
using TitaniumAS.Opc.Client.Da.Browsing.Internal;
using TitaniumAS.Opc.Client.Da.Wrappers;
using TitaniumAS.Opc.Client.Interop.Da;
using TitaniumAS.Opc.Client.Interop.Helpers;

namespace TitaniumAS.Opc.Client.Da.Browsing
{
    /// <summary>
    /// Represents an OPC DA 2.05 browser.
    /// </summary>
    /// <seealso cref="TitaniumAS.Opc.Da.Browsing.IOpcDaBrowser" />
    public class OpcDaBrowser2 : IOpcDaBrowser
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private OpcDaServer _opcDaServer;
        protected OpcBrowseServerAddressSpace OpcBrowseServerAddressSpace { get; set; }
        private OpcItemProperties OpcItemProperties { get; set; }

        /// <summary>
        /// Gets or sets the OPC DA server for browsing.
        /// </summary>
        /// <value>
        /// The OPC DA server.
        /// </value>
        public OpcDaServer OpcDaServer
        {
            get { return _opcDaServer; }
            set
            {
                _opcDaServer = value;
                if (value == null)
                {
                    OpcBrowseServerAddressSpace = null;
                    OpcItemProperties = null;
                }
                else
                {
                    OpcBrowseServerAddressSpace = _opcDaServer.As<OpcBrowseServerAddressSpace>();
                    OpcItemProperties = _opcDaServer.As<OpcItemProperties>();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpcDaBrowser2"/> class.
        /// </summary>
        /// <param name="opcDaServer">The OPC DA server for browsing.</param>
        public OpcDaBrowser2(OpcDaServer opcDaServer = null)
        {
            OpcDaServer = opcDaServer;
        }


        /// <summary>
        /// Browse the server using specified criteria.
        /// </summary>
        /// <param name="parentItemId">Indicates the item identifier from which browsing will begin. If the root branch is to be browsed then a null should be passed.</param>
        /// <param name="filter">The filtering context.</param>
        /// <param name="propertiesQuery">The properties query.</param>
        /// <returns>
        /// Array of browsed elements.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">Interface IOPCBrowseServerAddressSpace not supported.</exception>
        virtual public OpcDaBrowseElement[] GetElements(string parentItemId, OpcDaElementFilter filter = null,
            OpcDaPropertiesQuery propertiesQuery = null)
        {
            if (OpcBrowseServerAddressSpace == null)
                throw new InvalidOperationException("Interface IOPCBrowseServerAddressSpace not supported.");

            if (parentItemId == null)
                parentItemId = string.Empty;

            if (filter == null)
                filter = new OpcDaElementFilter();

            var elements = GetElementsImpl(parentItemId, filter);

            if (propertiesQuery != null)
            {
                var lst = new List<string>();
                foreach (var item in elements)
                {
                    lst.Add(item.ItemId);
                }
                var properties = GetProperties(lst.ToArray(), propertiesQuery);
                for (var i = 0; i < elements.Length; i++)
                {
                    elements[i].ItemProperties = properties[i];
                }
            }
            else
            {
                for (var i = 0; i < elements.Length; i++)
                {
                    elements[i].ItemProperties = OpcDaItemProperties.CreateEmpty();
                }
            }

            return elements;
        }

        /// <summary>
        /// Gets properties of items by specified item identifiers.
        /// </summary>
        /// <param name="itemIds">The item identifiers.</param>
        /// <param name="propertiesQuery">The properties query.</param>
        /// <returns>
        /// Array of properties of items.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">Interface IOPCItemProperties not supported.</exception>
        public OpcDaItemProperties[] GetProperties(IList<string> itemIds, OpcDaPropertiesQuery propertiesQuery = null)
        {
            if (OpcItemProperties == null)
                throw new InvalidOperationException("Interface IOPCItemProperties not supported.");

            if (propertiesQuery == null)
                propertiesQuery = new OpcDaPropertiesQuery();

            var result = new OpcDaItemProperties[itemIds.Count];

            for (var i = 0; i < result.Length; i++)
            {
                var itemId = itemIds[i];
                try
                {
                    OpcDaItemProperties itemProperties = OpcItemPropertiesExtensions.QueryAvailableProperties(OpcItemProperties, itemId);
                    if (!propertiesQuery.AllProperties) // filter properties
                    {
                        itemProperties.IntersectProperties(propertiesQuery.PropertyIds);
                    }
                    if (propertiesQuery.ReturnValues) // read property values
                    {
                        OpcItemPropertiesExtensions.GetItemProperties(OpcItemProperties, itemId, itemProperties);
                    }
                    OpcItemPropertiesExtensions.LookupItemIDs(OpcItemProperties, itemId, itemProperties);
                    result[i] = itemProperties;
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Cannot get properties for item '{0}'.", ex, itemId);
                    result[i] = OpcDaItemProperties.CreateEmpty();
                }
            }

            return result;
        }

        private OpcDaBrowseElement[] GetElementsImpl(string itemId, OpcDaElementFilter filter)
        {
            List<OpcDaBrowseElement> elements = new List<OpcDaBrowseElement>();
            var namespaceType = OpcBrowseServerAddressSpace.Organization;
            VarEnum dataTypeFilter = TypeConverter.ToVarEnum(filter.DataType);
            switch (namespaceType)
            {
                case OpcDaNamespaceType.Hierarchial:
                    ChangeBrowsePositionTo(itemId);

                    switch (filter.ElementType)
                    {
                        case OpcDaBrowseFilter.All:
                            var branches = OpcBrowseServerAddressSpace.BrowseOpcItemIds(OpcDaBrowseType.Branch,
                                filter.Name, dataTypeFilter,
                                filter.AccessRights);
                            foreach (var item in branches)
                            {
                                elements.Add(CreateBranchBrowseElement(item));
                            }
                            var leafs = OpcBrowseServerAddressSpace.BrowseOpcItemIds(OpcDaBrowseType.Leaf, filter.Name,
                                dataTypeFilter,
                                filter.AccessRights);
                            foreach (var item in leafs)
                            {
                                elements.Add(CreateLeafBrowseElement(item));
                            }
                            break;
                        case OpcDaBrowseFilter.Branches:
                            var ids = OpcBrowseServerAddressSpace.BrowseOpcItemIds(OpcDaBrowseType.Branch, filter.Name,
                                dataTypeFilter,
                                filter.AccessRights);
                            foreach (var item in ids)
                            {
                                elements.Add(CreateBranchBrowseElement(item));
                            }
                            break;
                        case OpcDaBrowseFilter.Items:
                            var ids2 = OpcBrowseServerAddressSpace.BrowseOpcItemIds(OpcDaBrowseType.Leaf, filter.Name,
                                dataTypeFilter,
                                filter.AccessRights);
                            foreach (var item in ids2)
                            {
                                elements.Add(CreateLeafBrowseElement(item));
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                case OpcDaNamespaceType.Flat:
                    if (filter.ElementType == OpcDaBrowseFilter.Branches) // no branches in flat namespace
                    {
                        //elements = new List<OpcDaBrowseElement>();
                    }
                    else
                    {
                        var ids3 = OpcBrowseServerAddressSpace.BrowseOpcItemIds(OpcDaBrowseType.Flat, filter.Name,
                            dataTypeFilter,
                            filter.AccessRights);
                        foreach (var item in ids3)
                        {
                            elements.Add(CreateLeafBrowseElement(item));
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return elements.ToArray();
        }

        protected virtual void ChangeBrowsePositionTo(string itemId)
        {
            OpcBrowseServerAddressSpace.ChangeBrowsePosition(OPCBROWSEDIRECTION.OPC_BROWSE_TO, itemId);
        }

        private OpcDaBrowseElement CreateLeafBrowseElement(string name)
        {
            return new OpcDaBrowseElement
            {
                Name = name,
                HasChildren = false,
                IsItem = true,
                ItemId = OpcBrowseServerAddressSpaceExtensions.TryGetItemId(OpcBrowseServerAddressSpace, name)
            };
        }

        private OpcDaBrowseElement CreateBranchBrowseElement(string name)
        {
            return new OpcDaBrowseElement
            {
                Name = name,
                HasChildren = true,
                IsItem = false,
                ItemId = OpcBrowseServerAddressSpaceExtensions.TryGetItemId(OpcBrowseServerAddressSpace, name)
            };
        }
    }
}