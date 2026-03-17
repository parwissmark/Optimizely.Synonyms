using EPiServer.Shell.Navigation;

namespace Synonyms.Plugins.Synonyms;

[MenuProvider]
public class SynonymsMenuProvider : IMenuProvider
{
    public IEnumerable<MenuItem> GetMenuItems()
    {
        return
        [
            new UrlMenuItem("Synonyms manager", MenuPaths.Global + "/cms/synonyms", "/plugins/synonyms")
            {
                SortIndex = 100
            }
        ];
    }
}
