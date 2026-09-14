export const GRAPH_PAGE_SIZE = 25;
export const GRAPH_MAX_DEPTH = 2;

export function normalizeRelationship(rel, index = 0) {
  const from = rel.from?.Id || rel.from?.id || rel.From?.Id || rel.fromId || rel.FromId || rel.from;
  const to = rel.to?.Id || rel.to?.id || rel.To?.Id || rel.toId || rel.ToId || rel.to;
  const fromId = typeof from === 'object' ? from?.Id || from?.id : from;
  const toId = typeof to === 'object' ? to?.Id || to?.id : to;
  return {
    key: rel.id || rel.Id || `edge-${index}`,
    fromId: String(fromId || ''),
    toId: String(toId || ''),
    fromName: rel.from?.CanonicalName || rel.from?.canonicalName || rel.From?.CanonicalName || String(fromId || ''),
    toName: rel.to?.CanonicalName || rel.to?.canonicalName || rel.To?.CanonicalName || String(toId || ''),
    type: rel.type || rel.Type || 'related',
    certainty: rel.certainty || rel.Certainty || 'Unknown',
  };
}

export function buildNeighborhood(centerId, relationships) {
  const edges = (relationships || []).map(normalizeRelationship);
  const relevant = edges.filter(edge => edge.fromId === centerId || edge.toId === centerId);
  const nodes = new Map();
  nodes.set(centerId, { id: centerId, center: true });
  for (const edge of relevant) {
    const otherId = edge.fromId === centerId ? edge.toId : edge.fromId;
    const otherName = edge.fromId === centerId ? edge.toName : edge.fromName;
    if (!nodes.has(otherId)) nodes.set(otherId, { id: otherId, name: otherName });
  }
  return { nodes: [...nodes.values()], edges: relevant };
}

export function paginateEdges(edges, { pageSize = GRAPH_PAGE_SIZE, showAll = false } = {}) {
  const list = edges || [];
  return {
    visible: showAll ? list : list.slice(0, pageSize),
    hasMore: !showAll && list.length > pageSize,
    total: list.length,
  };
}

export function disputedGroups(edges) {
  const groups = new Map();
  for (const edge of edges || []) {
    const pair = [edge.fromId, edge.toId].sort().join('|');
    const key = `${pair}|${edge.type}`;
    if (!groups.has(key)) groups.set(key, []);
    groups.get(key).push(edge);
  }
  return [...groups.values()].filter(group => group.length > 1);
}
