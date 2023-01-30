﻿namespace FormularPortal.Core.Models
{
    public abstract class FormElementWithOptions : FormElement
    {
        public List<FormElementOption> Options { get; set; } = new List<FormElementOption>();
    }
}
