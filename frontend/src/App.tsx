import { Routes, Route, Link } from "react-router-dom";
import Home from "./pages/Home";
import Auth from "./pages/Auth";
import Utilities from "./pages/Utilities";
import Success from "./pages/Success";

export default function App() {
  return (
    <div>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/auth" element={<Auth />} />
        <Route path="/utilities" element={<Utilities />} />
        <Route path="/success" element={<Success />} />
      </Routes>
    </div>
  );
}
