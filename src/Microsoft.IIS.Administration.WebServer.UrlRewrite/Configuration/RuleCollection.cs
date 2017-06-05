namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using System;
    using Web.Administration;

    public class RuleCollection : ConfigurationElementCollectionBase<Rule>
    {
        public RuleCollection() { }

        public new Rule this[string name] {
            get {
                for (int i = 0; (i < this.Count); i = (i + 1)) {
                    Rule element = base[i];
                    if ((string.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase) == true)) {
                        return element;
                    }
                }
                return null;
            }
        }

        public Rule Add(string name)
        {
            Rule element = this.CreateElement();
            element.Name = name;
            return base.Add(element);
        }

        protected override Rule CreateNewElement(string elementTagName)
        {
            return new Rule();
        }
    }
}
