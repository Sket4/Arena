using Unity.Entities;
using UnityEngine;
using System;
using System.Threading.Tasks;
using TzarGames.GameCore.Baking;
using UnityEngine.Localization.Tables;

namespace TzarGames.GameCore.Client
{
    [Serializable]
    public struct Description : ISharedComponentData, IEquatable<Description>
    {
        private const string notLocalized = "NOT LOCALIZED";
        public UnityEngine.Localization.LocalizedString Value;

        public bool Equals(Description other)
        {
            return other.Value == Value;
        }

        public override int GetHashCode()
        {
            return (Value != null) ? Value.TableEntryReference.GetHashCode() : 0;
        }

        public override string ToString()
        {
            var task = ToStringAsync();
            task.Wait();
            return task.Result;
        }

        public async Task<string> ToStringAsync() 
        {
            if(Value == null)
            {
                return notLocalized;
            }
            if (Value.IsEmpty)
            {
                var tableRef = Value.TableReference;
                if (tableRef.ReferenceType == TableReference.Type.Empty)
                {
                    tableRef.OnAfterDeserialize();
                    Value.TableReference = tableRef;
                }

                var tableEntryRef = Value.TableEntryReference;  
                if (tableEntryRef.ReferenceType == TableEntryReference.Type.Empty)
                {
                    tableEntryRef.OnAfterDeserialize();
                    Value.TableEntryReference = tableEntryRef;
                }
                    
                Value.OnAfterDeserialize();
            }

            var result = await Value.GetLocalizedStringAsync().Task;
            return result;
        }
    }

    [DisallowMultipleComponent]
    public class DescriptionComponent : ManagedSharedComponentDataBehaviour<Description>
    {
        protected override ConversionTargetOptions GetDefaultConversionOptions()
        {
            return LocalAndClient();
        }
    }    
}

