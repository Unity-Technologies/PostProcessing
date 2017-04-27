using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.PostProcessing
{
    public sealed class PropertySheetFactory
    {
        readonly Dictionary<string, PropertySheet> m_Sheets;

        public PropertySheetFactory()
        {
            m_Sheets = new Dictionary<string, PropertySheet>();
        }

        public PropertySheet Get(string shaderName)
        {
            PropertySheet sheet;

            if (m_Sheets.TryGetValue(shaderName, out sheet))
                return sheet;

            var shader = Shader.Find(shaderName);

            if (shader == null)
                throw new ArgumentException(string.Format("Invalid shader ({0})", shaderName));

            var material = new Material(shader)
            {
                name = string.Format("PostProcess - {0}", shaderName.Substring(shaderName.LastIndexOf('/') + 1)),
                hideFlags = HideFlags.DontSave
            };

            sheet = new PropertySheet(material);
            m_Sheets.Add(shaderName, sheet);
            return sheet;
        }

        public void Release()
        {
            foreach (var sheet in m_Sheets.Values)
                sheet.Release();

            m_Sheets.Clear();
        }
    }
}
