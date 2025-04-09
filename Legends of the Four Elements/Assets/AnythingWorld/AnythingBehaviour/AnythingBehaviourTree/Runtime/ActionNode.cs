namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// An abstract base class for action nodes within a behavior tree. Action nodes are the leaf nodes of the tree
    /// where the actual actions or tasks are executed. These nodes don't have children and are responsible
    /// for performing specific operations, like moving an agent or executing an animation.
    /// </summary>
    [System.Serializable]
    public abstract class ActionNode : Node {}
}
