import { Check, Circle } from "lucide-react";
import type { WorkflowStep, WorkflowStepStatus } from "../../domain";
import { workflowStatusLabels } from "./formatters";

export function Workflow({ steps, updatingSteps, onChange }: { steps: WorkflowStep[]; updatingSteps: ReadonlySet<string>; onChange: (step: WorkflowStep, status: WorkflowStepStatus) => void }) {
  const completed = steps.filter(step => step.status === "Completed" || step.status === "NotRequired").length;
  return <div className="workflow"><div className="section-heading"><div><p className="eyebrow">CHECKLIST ZAKÁZKY</p><h3>Průběh vyřízení</h3></div><span>{completed}/{steps.length}</span></div>{steps.map((step, index) => <WorkflowRow key={step.id} step={step} index={index} updating={updatingSteps.has(step.id)} onChange={onChange} />)}</div>;
}

function WorkflowRow({ step, index, updating, onChange }: { step: WorkflowStep; index: number; updating: boolean; onChange: (step: WorkflowStep, status: WorkflowStepStatus) => void }) {
  return <article className={`workflow-step ${step.status === "Completed" ? "done" : ""}`}><button className="step-check" disabled={updating} onClick={() => onChange(step, step.status === "Completed" ? "Pending" : "Completed")} title="Změnit stav">{step.status === "Completed" ? <Check size={16} /> : <Circle size={15} />}</button><div className="step-number">{index + 1}</div><div className="step-copy"><b>{step.title}</b><p>{step.description}</p>{step.notes && <small>{step.notes}</small>}</div><select disabled={updating} value={step.status} onChange={event => onChange(step, event.target.value as WorkflowStepStatus)}>{Object.entries(workflowStatusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></article>;
}
