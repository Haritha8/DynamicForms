using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DynamicForms.Models.Data;
using DynamicForms.ViewModels;
namespace DynamicForms.Services
{
    public class FormActionService
    {
        // ---- PUBLIC ENTRY POINTS ----
        public async Task SaveNewEntryAsync(FormViewModel root, ActionViewModel actionVm)
        {
            // 1. Find all fields whose BindingPath is under "data.currentEntry"
            var fields = GetAllFields(root)
               .Where(f => !string.IsNullOrEmpty(f.BindingPath) &&
                           f.BindingPath.StartsWith("data.currentEntry."))
               .ToList();
            if (fields.Count == 0)
                return;
            // 2. VALIDATION: check required fields
            foreach (var f in fields)
            {
                if (!f.IsRequired)
                    continue;
                var val = f.Value?.ToString();
                if (string.IsNullOrWhiteSpace(val))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Validation failed: {f.Label} is required. Message: {f.ValidationErrorMessage}");
                    return; // cancel save
                }
            }
            var saveCfg = actionVm.SaveConfig;
            if (saveCfg == null)
            {
                System.Diagnostics.Debug.WriteLine("SaveNewEntry clicked but saveConfig is null.");
                return;
            }
            // 3. Build a new log entry JObject from the current entry fields
            var logEntry = new Newtonsoft.Json.Linq.JObject();
            foreach (var f in fields)
            {
                if (string.IsNullOrEmpty(f.BindingPath))
                    continue;
                // BindingPath like "data.currentEntry.toolingSetUsed" → take last segment
                var segments = f.BindingPath.Split('.');
                var propName = segments[segments.Length - 1];
                var valueToken = f.Value != null
                    ? Newtonsoft.Json.Linq.JToken.FromObject(f.Value)
                    : Newtonsoft.Json.Linq.JValue.CreateNull();
                logEntry[propName] = valueToken;
            }
            // default flag for edit mode in the log
            logEntry["isEditing"] = false;
            // 4. Append to data.logs
            var logsToken = actionVm.DataContext.GetToken(saveCfg.TargetCollectionPath)
                              as Newtonsoft.Json.Linq.JArray;
            if (logsToken == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Target collection '{saveCfg.TargetCollectionPath}' is not a JArray.");
                return;
            }
            // Determine current ToolSetup# from the corresponding field
            var numberField = fields.FirstOrDefault(f =>
                f.BindingPath?.EndsWith("." + saveCfg.AutoIncrementField) == true);
            int currentNumber = 1;
            if (numberField?.Value != null)
            {
                if (numberField.Value is int i) currentNumber = i;
                else if (numberField.Value is long l) currentNumber = (int)l;
                else int.TryParse(numberField.Value.ToString(), out currentNumber);
            }
            // Ensure the log entry uses the current number
            logEntry[saveCfg.AutoIncrementField] = currentNumber;
            logsToken.Add(logEntry);
            // 5. Auto-increment counter and currentEntry.toolSetupNumber for the NEXT row
            int nextNumber = currentNumber + 1;
            // Update the meta counter in JSON
            actionVm.DataContext.SetValue(saveCfg.CounterPath, nextNumber);
            // Update the currentEntry field via the ViewModel (this also updates JSON)
            if (numberField != null)
            {
                numberField.Value = nextNumber;
            }
            // 6. Reset other entry fields if resetAfterSave == true
            if (saveCfg.ResetAfterSave)
            {
                foreach (var f in fields)
                {
                    if (f == numberField)
                        continue; // don't reset the auto-increment field
                    switch (f.DataType)
                    {
                        case "Int":
                            f.Value = null;
                            break;
                        case "Bool":
                            f.Value = false;
                            break;
                        default:
                            f.Value = string.Empty;
                            break;
                    }
                }
            }
            await Task.CompletedTask;
        }
        public async Task SaveRowEditAsync(FormViewModel root, ActionViewModel actionVm)
        {
            // 1. Get all fields that belong to this row (same FormDataContext)
            var rowFields = GetFieldsForSameContext(root, actionVm.DataContext).ToList();
            if (rowFields.Count == 0)
                return;
            // 2. Optional: validate required fields in this row
            foreach (var f in rowFields)
            {
                if (!f.IsRequired)
                    continue;
                var val = f.Value?.ToString();
                if (string.IsNullOrWhiteSpace(val))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Row validation failed: {f.Label} is required. Message: {f.ValidationErrorMessage}");
                    return; // cancel save for this row
                }
            }
            // 3. Flip isEditing = false for this row's JSON object
            // Because actionVm.DataContext is a row context (basePath "data.logs[i]"),
            // "isEditing" resolves to "data.logs[i].isEditing"
            actionVm.DataContext.SetValue("isEditing", false);
            await Task.CompletedTask;
        }
        public void EnterEditMode(ActionViewModel actionVm)
        {
            // Very small rule, but still "business" (change JSON state)
            actionVm.DataContext.SetValue("isEditing", true);
        }
        // ---- PRIVATE HELPERS ----
        private IEnumerable<FieldViewModel> GetAllFields(ElementViewModel root)
        {
            if (root is FieldViewModel fv)
                yield return fv;
            if (root is FormViewModel formVm)
            {
                foreach (var child in formVm.Children)
                {
                    foreach (var c in GetAllFields(child))
                        yield return c;
                }
            }
            else if (root is SectionViewModel sectionVm)
            {
                foreach (var child in sectionVm.Children)
                {
                    foreach (var c in GetAllFields(child))
                        yield return c;
                }
            }
            else if (root is RepeaterViewModel repeaterVm)
            {
                foreach (var item in repeaterVm.Items)
                {
                    foreach (var child in item.Children)
                    {
                        foreach (var c in GetAllFields(child))
                            yield return c;
                    }
                }
            }
        }
        private IEnumerable<FieldViewModel> GetFieldsForSameContext(
            FormViewModel root,
            FormDataContext ctx)
        {
            return GetAllFields(root)
                .Where(f => ReferenceEquals(f.DataContext, ctx));
        }
    }
}