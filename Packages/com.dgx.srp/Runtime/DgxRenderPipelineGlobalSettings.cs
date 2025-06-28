using System;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
#endif

namespace DGX.SRP
{
    /// <summary>
    /// DGX Render Pipeline's Global Settings.
    /// Global settings are unique per Render Pipeline type
    /// </summary>
    [DisplayInfo(name = "DGX Global Settings Asset", order = CoreUtils.Sections.section4 + 2)]
    [SupportedOnRenderPipeline(typeof(RenderPipelineAsset))]
    [DisplayName("DGX")]
    partial class DgxRenderPipelineGlobalSettings : RenderPipelineGlobalSettings<DgxRenderPipelineGlobalSettings, RenderPipeline>
    {
        [SerializeField] RenderPipelineGraphicsSettingsContainer m_Settings = new();
        protected override List<IRenderPipelineGraphicsSettings> settingsList => m_Settings.settingsList;
    }
}
