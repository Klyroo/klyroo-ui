using Blux.UI.Tools.Query;

namespace Blux.UI.Tools;

/// <summary>Which rows of a hierarchical tree table can be selected with checkboxes.</summary>
public enum DataListTreeSelectionMode
{
    /// <summary>Every row in the tree (roots and descendants) has its own independent selection checkbox.</summary>
    All,

    /// <summary>Only top-level root rows (depth 0) have a selection checkbox; descendant rows reserve the selection column width.</summary>
    Roots,
}

/// <summary>
/// One node in the flattened hierarchy produced by <see cref="DataListTree{TItem}"/> over the loaded top-level rows of a data list.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed class DataListTreeNode<TItem>
{
    internal DataListTreeNode(
        string key,
        TItem item,
        int depth,
        DataListTreeNode<TItem>? parent,
        int rootIndex,
        bool isLast,
        IReadOnlyList<bool> ancestorContinuations)
    {
        Key = key;
        Item = item;
        Depth = depth;
        Parent = parent;
        RootIndex = rootIndex;
        IsLast = isLast;
        AncestorContinuations = ancestorContinuations;
    }

    /// <summary>Unique key of the row across the tree.</summary>
    public string Key { get; }

    /// <summary>The row data item.</summary>
    public TItem Item { get; }

    /// <summary>Nesting depth: <c>0</c> for top-level root rows, <c>1</c> for immediate children, etc.</summary>
    public int Depth { get; }

    /// <summary>Parent tree node, or <see langword="null"/> for top-level root rows.</summary>
    public DataListTreeNode<TItem>? Parent { get; }

    /// <summary>Zero-based index of the top-level root ancestor.</summary>
    public int RootIndex { get; }

    /// <summary>Child tree nodes.</summary>
    public IReadOnlyList<DataListTreeNode<TItem>> Children { get; internal set; } = [];

    /// <summary>Whether this node has at least one child node.</summary>
    public bool HasChildren => Children.Count > 0;

    /// <summary>Whether this node is the last sibling of its parent.</summary>
    public bool IsLast { get; }

    /// <summary>
    /// For each ancestor depth from <c>1</c> to <c>Depth - 1</c>, whether that ancestor has further siblings below it
    /// (used to draw vertical continuation rails).
    /// </summary>
    public IReadOnlyList<bool> AncestorContinuations { get; }
}

/// <summary>
/// Hierarchical tree configuration for <see cref="BxDataListTable{TItem}"/>.
/// Child rows travel with their parent item (<see cref="Children"/>), expand and collapse with a chevron button (mouse, Enter/Space, or ArrowRight/ArrowLeft)
/// and visual rail connectors, and participate in row selection according to <see cref="Selectable"/>.
/// When a tree is configured on <see cref="BxDataListTable{TItem}"/>, summary rows and visual groups are disabled on the table.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed class DataListTree<TItem>
{
    /// <summary>
    /// Extracts the child items of a row. Null items and nodes whose key duplicates an earlier node in the tree are skipped.
    /// </summary>
    public required Func<TItem, IEnumerable<TItem>?> Children { get; init; }

    /// <summary>
    /// Whether all branch nodes start expanded by default (<see langword="false"/> by default).
    /// When <see cref="DefaultExpandedDepth"/> is set, it takes precedence over this flag.
    /// </summary>
    public bool DefaultExpanded { get; init; }

    /// <summary>
    /// Optional number of nesting levels expanded by default (<c>0</c> = none, <c>1</c> = root branches expanded, <c>2</c> = roots and their children expanded, etc.).
    /// Overrides <see cref="DefaultExpanded"/> when not <see langword="null"/>.
    /// </summary>
    public int? DefaultExpandedDepth { get; init; }

    /// <summary>
    /// Which rows in the tree display a selection checkbox (<see cref="DataListTreeSelectionMode.All"/> by default).
    /// </summary>
    public DataListTreeSelectionMode Selectable { get; init; } = DataListTreeSelectionMode.All;

    /// <summary>Whether a branch node at <paramref name="depth"/> starts expanded according to the baseline settings.</summary>
    public bool IsDefaultExpanded(int depth) =>
        DefaultExpandedDepth is { } maxDepth ? depth < maxDepth : DefaultExpanded;

    internal (IReadOnlyList<DataListTreeNode<TItem>> Roots, IReadOnlyList<DataListTreeNode<TItem>> AllNodes) BuildTree(
        IReadOnlyList<TItem> rootItems,
        DataListSchema<TItem>? schema)
    {
        if (rootItems.Count == 0) return ([], []);

        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        var allNodes = new List<DataListTreeNode<TItem>>(rootItems.Count * 2);
        var autoIndex = 0;

        string ResolveKey(TItem item)
        {
            autoIndex++;
            if (schema is { HasRowKey: true })
            {
                var k = schema.GetRowKey(item);
                if (!string.IsNullOrEmpty(k)) return k;
            }
            return "#" + autoIndex;
        }

        List<DataListTreeNode<TItem>> BuildLevel(
            IReadOnlyList<TItem> items,
            int depth,
            DataListTreeNode<TItem>? parent,
            int rootIndex,
            IReadOnlyList<bool> continuations)
        {
            // Filter out nulls and duplicate keys first so IsLast is accurate for the remaining siblings.
            var valid = new List<(string Key, TItem Item)>(items.Count);
            foreach (var item in items)
            {
                if (item is null) continue;
                var key = ResolveKey(item);
                if (!seenKeys.Add(key)) continue;
                valid.Add((key, item));
            }

            var nodes = new List<DataListTreeNode<TItem>>(valid.Count);
            for (var i = 0; i < valid.Count; i++)
            {
                var (key, item) = valid[i];
                var isLast = i == valid.Count - 1;
                var currentRootIndex = depth == 0 ? i : rootIndex;
                var node = new DataListTreeNode<TItem>(key, item, depth, parent, currentRootIndex, isLast, continuations);
                allNodes.Add(node);

                var rawChildren = Children(item)?.Where(c => c is not null).ToList();
                if (rawChildren is { Count: > 0 })
                {
                    IReadOnlyList<bool> childContinuations = depth == 0
                        ? []
                        : [.. continuations, !isLast];
                    node.Children = BuildLevel(rawChildren, depth + 1, node, currentRootIndex, childContinuations);
                }

                nodes.Add(node);
            }
            return nodes;
        }

        var roots = BuildLevel(rootItems, 0, null, 0, []);
        return (roots, allNodes);
    }

    internal static IReadOnlyList<DataListTreeNode<TItem>> FlattenVisible(
        IReadOnlyList<DataListTreeNode<TItem>> roots,
        Func<DataListTreeNode<TItem>, bool> isExpanded)
    {
        if (roots.Count == 0) return [];
        var result = new List<DataListTreeNode<TItem>>(roots.Count * 2);

        void Walk(IReadOnlyList<DataListTreeNode<TItem>> level)
        {
            foreach (var node in level)
            {
                result.Add(node);
                if (node.HasChildren && isExpanded(node))
                {
                    Walk(node.Children);
                }
            }
        }

        Walk(roots);
        return result;
    }
}
