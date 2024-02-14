using JiraDriver;
using Microsoft.Graph.Beta.DeviceManagement.DeviceConfigurations.Item.GetOmaSettingPlainTextValueWithSecretReferenceValueId;

namespace CallMSGraph.Models
{
    public class CalendarioModel : EventModel
    {
        public string Subject { get; set; }

        public JiraWorkLog JiraWorkLog { get; set; }
    }
}
