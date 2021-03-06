//-------------------------------------------------------------------------------------------------
// <copyright file="VariableInfo.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Utility class for Burn variable information.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset
{
    using System;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    /// <summary>
    /// Utility class for Burn variable information.
    /// </summary>
    internal class VariableInfo
    {
        private VariableRow variableRow;

        public VariableInfo(VariableRow variableRow)
        {
            this.variableRow = variableRow;
        }

        /// <summary>
        /// Gets or sets whether this variable is hidden.
        /// </summary>
        /// <value>Whether this variable is hidden.</value>
        public bool Hidden
        {
            get { return this.variableRow.Hidden; }
            private set { this.variableRow.Hidden = value; }
        }

        /// <summary>
        /// Gets or sets the variable identifier.
        /// </summary>
        /// <value>The variable identifier.</value>
        public string Id
        {
            get { return this.variableRow.Id; }
            private set { this.variableRow.Id = value; }
        }

        /// <summary>
        /// Gets or sets whether this variable is persisted.
        /// </summary>
        /// <value>Whether this variable is persisted.</value>
        public bool Persisted
        {
            get { return this.variableRow.Persisted; }
            private set { this.variableRow.Persisted = value; }
        }

        /// <summary>
        /// Gets or sets the variable's value.
        /// </summary>
        /// <value>The variable's value.</value>
        public string Value
        {
            get { return this.variableRow.Value; }
            private set { this.variableRow.Value = value; }
        }

        /// <summary>
        /// Gets or sets the variable's type.
        /// </summary>
        /// <value>The variable's type.</value>
        public string Type
        {
            get { return this.variableRow.Type; }
            private set { this.variableRow.Type = value; }
        }

        /// <summary>
        /// Generates Burn manifest markup for a variable.
        /// </summary>
        /// <param name="writer"></param>
        public void WriteXml(XmlTextWriter writer)
        {
            writer.WriteStartElement("Variable");
            writer.WriteAttributeString("Id", this.Id);
            if (null != this.Type)
            {
                writer.WriteAttributeString("Value", this.Value);
                writer.WriteAttributeString("Type", this.Type);
            }
            writer.WriteAttributeString("Hidden", this.Hidden ? "yes" : "no");
            writer.WriteAttributeString("Persisted", this.Persisted ? "yes" : "no");
            writer.WriteEndElement();
        }
    }
}
