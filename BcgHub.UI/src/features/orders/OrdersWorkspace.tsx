import { OrderDetailView } from "./OrderDetailView";
import { OrderListPane } from "./OrderListPane";
import { useOrdersWorkspace } from "./useOrdersWorkspace";

export function OrdersWorkspace() {
  const state = useOrdersWorkspace();
  return <div className="split-view"><OrderListPane {...state} /><OrderDetailView order={state.detail} updatingSteps={state.updatingSteps} onStepChange={state.updateStep} /></div>;
}
