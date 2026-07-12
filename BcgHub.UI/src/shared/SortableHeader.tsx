export function SortableHeader({ label, active, descending, onSort }: { label: string; active: boolean; descending: boolean; onSort: () => void }) {
  return <th><button type="button" onClick={onSort}>{label}<span className={active ? "active" : ""}>{active ? descending ? "↓" : "↑" : "↕"}</span></button></th>;
}
