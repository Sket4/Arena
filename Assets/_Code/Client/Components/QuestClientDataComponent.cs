using System;
using TzarGames.GameCore;
using TzarGames.GameCore.Baking;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Arena.Client
{
    [System.Serializable]
    public sealed class LocationClientData : IComponentData
    {
        public UnityEngine.Localization.LocalizedString Name;
        public UnityEngine.Localization.LocalizedString Description;
    }

    public static class LocalizedStringExtensions
    {
        public static string TryGetLocalizedString(this UnityEngine.Localization.LocalizedString loc)
        {
            if (loc == null
                //|| loc.IsEmpty
                )
            {
                return "error";
            }

            try
            {
                if (loc.IsEmpty)
                {
                    var tableRef = loc.TableReference;
                    if (tableRef.ReferenceType == TableReference.Type.Empty)
                    {
                        tableRef.OnAfterDeserialize();
                        loc.TableReference = tableRef;
                    }

                    var tableEntryRef = loc.TableEntryReference;  
                    if (tableEntryRef.ReferenceType == TableEntryReference.Type.Empty)
                    {
                        tableEntryRef.OnAfterDeserialize();
                        loc.TableEntryReference = tableEntryRef;
                    }
                    
                    loc.OnAfterDeserialize();
                }
                
                return loc.GetLocalizedString();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return "error";
            }
        }
    }
    
    public class QuestClientDataComponent : ComponentDataClassBehaviour<LocationClientData>
    {
        protected override ConversionTargetOptions GetDefaultConversionOptions()
        {
            return LocalAndClient();
        }
    }
}
