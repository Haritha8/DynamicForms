using DynamicForms.Models.Data;

using DynamicForms.Models.Definitions;

namespace DynamicForms.ViewModels

{
    public static class ElementViewModelFactory

    {

        public static ElementViewModel Create(FormElementDefinition definition, FormDataContext ctx)

        {

            switch (definition.ElementType)

            {

                case "Form":

                    return new FormViewModel((FormDefinition)definition, ctx);

                case "Section":

                    return new SectionViewModel((SectionDefinition)definition, ctx);

                case "Repeater":

                    return new RepeaterViewModel((RepeaterDefinition)definition, ctx);

                case "Field":

                    return new FieldViewModel((FieldDefinition)definition, ctx);

                case "Action":

                    // we’ll wire real actions (save, edit, etc.) in the next step

                    return new ActionViewModel((ActionDefinition)definition, ctx, null);

                default:

                    return null;

            }

        }

    }

}
