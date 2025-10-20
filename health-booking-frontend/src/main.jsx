import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App.jsx";       // default import expects 'export default' in App.jsx

ReactDOM.createRoot(document.getElementById("root")).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
