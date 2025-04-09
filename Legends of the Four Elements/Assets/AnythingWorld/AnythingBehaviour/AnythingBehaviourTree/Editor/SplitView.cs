using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// A custom UI element that allows for more flexible layout arrangements by splitting the space into
    /// two resizable panes.
    /// </summary>
    public class SplitView : TwoPaneSplitView
    {
        public new class UxmlFactory : UxmlFactory<SplitView, TwoPaneSplitView.UxmlTraits>{}
    }
}
