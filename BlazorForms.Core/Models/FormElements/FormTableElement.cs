﻿using BlazorForms.Core.Constants;
using DbController;

namespace BlazorForms.Core.Models.FormElements
{
    public class FormTableElement : FormElement
    {
        [CompareField("allow_add_rows")]
        public bool AllowAddRows { get; set; }
        /// <summary>
        /// Holds all elements for the FormEditor
        /// </summary>
        public List<FormElement> Elements { get; set; } = [];

        /// <summary>
        /// Holds all values for a FormEntry.
        /// </summary>
        public List<List<FormElement>> ElementValues { get; set; } = [];
        public override ElementType GetElementType() => ElementType.Table;
        public override string GetDefaultName() => "Table";

        public override Dictionary<string, object?> GetParameters()
        {
            var parameters = base.GetParameters();

            parameters.Add("ALLOW_ADD_ROWS", AllowAddRows);

            return parameters;
        }

        /// <summary>
        /// Adds a new row to fill out for the user.
        /// </summary>
        public List<FormElement> NewRow()
        {
            //var tmp = Elements.DeepCopyByExpressionTree();

            List<FormElement> tmp = [];
            Guid guid = Guid.NewGuid();
            foreach (var element in Elements)
            {
                var copy = (FormElement)element.Clone();
                copy.GuidTableCount = guid;
                if (copy is FormDateElement dateElement)
                {
                    if (dateElement.SetDefaultValueToCurrentDate)
                    {
                        dateElement.Value = DateTime.Now;
                    }
                }

                tmp.Add(copy);
            }

            ElementValues.Add(tmp);
            return tmp;
        }

        public override void SetValue(FormEntryElement element)
        {
            // No value to be set
        }
        public override void Reset()
        {
            // This element has nothing to be resetted
        }

        public override object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
