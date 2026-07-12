import { Check, Circle } from "lucide-react";
import type { WorkflowStep, WorkflowStepStatus } from "../../domain";
import { workflowStatusLabels } from "./formatters";

export function Workflow({ steps, updatingSteps, onChange }: { steps: WorkflowStep[]; updatingSteps: ReadonlySet<string>; onChange: (step: WorkflowStep, status: WorkflowStepStatus) => void }) {
  const completed = steps.filter(step => step.status === "Completed" || step.status === "NotRequired").length;
  return <div className="workflow"><div className="section-heading"><div><p className="eyebrow">CHECKLIST ZAKÁZKY</p><h3>Průběh vyřízení</h3></div><span>{completed}/{steps.length}</span></div><div className="data-table-wrap embedded"><table className="data-table workflow-table"><thead><tr><th>#</th><th>Hotovo</th><th>Krok</th><th>Popis</th><th>Termín</th><th>Stav</th></tr></thead><tbody>{steps.map((step, index) => <WorkflowRow key={step.id} step={step} index={index} updating={updatingSteps.has(step.id)} onChange={onChange} />)}</tbody></table></div></div>;
}

function WorkflowRow({ step, index, updating, onChange }: { step: WorkflowStep; index: number; updating: boolean; onChange: (step: WorkflowStep, status: WorkflowStepStatus) => void }) {
  return <tr className={step.status === "Completed" ? "done" : ""}><td>{index + 1}</td><td className="icon-cell"><button className="step-check" disabled={updating} onClick={() => onChange(step, step.status === "Completed" ? "Pending" : "Completed")} title="Změnit stav">{step.status === "Completed" ? <Check size={16} /> : <Circle size={15} />}</button></td><td><strong>{step.title}</strong>{step.notes && <small className="cell-note">{step.notes}</small>}</td><td title={step.description}>{step.description}</td><td>{step.dueAtUtc ? new Date(step.dueAtUtc).toLocaleDateString("cs-CZ") : "—"}</td><td><select disabled={updating} value={step.status} onChange={event => onChange(step, event.target.value as WorkflowStepStatus)}>{Object.entries(workflowStatusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></td></tr>;
}
