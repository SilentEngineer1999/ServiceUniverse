import { Routes, Route, Link } from "react-router-dom";
import Home from "./pages/Home";
import Auth from "./pages/Auth";
import Utilities from "./pages/Utilities";
import Success from "./pages/Success";
import PassBuyAuth from "./pages/PassBuyAuth";
import PassBuyApply from "./pages/PassBuyApply";
import PassBuyFulfilment from "./pages/PassBuyFulfillment";
import PassBuyCards from "./pages/PassBuyCards";
import HealthApp from "./health/App";

export default function App() {
  return (
    <div>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/auth" element={<Auth />} />
        <Route path="/utilities" element={<Utilities />} />
        <Route path="/success" element={<Success />} />
        <Route path="/PassBuy/auth" element={<PassBuyAuth />} />
        <Route path="/PassBuy/apply" element={<PassBuyApply />} />
        <Route path="/PassBuy/fulfillment" element={<PassBuyFulfilment />} />
        <Route path="/PassBuy/cards" element={<PassBuyCards />} />
        <Route path="/Health" element={<HealthApp/>} />
      </Routes>
    </div>
  );
}
