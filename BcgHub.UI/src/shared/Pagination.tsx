import { ChevronLeft, ChevronRight } from "lucide-react";

interface Props { page: number; pageSize: number; totalCount: number; onPageChange: (page: number) => void }

export function Pagination({ page, pageSize, totalCount, onPageChange }: Props) {
  const pageCount = Math.max(1, Math.ceil(totalCount / pageSize));
  if (totalCount <= pageSize) return null;
  return <nav className="pagination" aria-label="Stránkování"><button className="secondary" disabled={page <= 1} onClick={() => onPageChange(page - 1)}><ChevronLeft size={16} /> Předchozí</button><span>Strana {page} z {pageCount}</span><button className="secondary" disabled={page >= pageCount} onClick={() => onPageChange(page + 1)}>Další <ChevronRight size={16} /></button></nav>;
}
