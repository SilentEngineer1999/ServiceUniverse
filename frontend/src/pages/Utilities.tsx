import { useEffect, useState } from "react";
import axios from "axios";
import {
  Box,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Link,
} from "@mui/material";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";

type Utility = {
  utilityId: number;
  userId: number;
  gasUsage: number;
  gasRate: number;
  waterUsage: number;
  waterRate: number;
  electricityUsage: number;
  electricityRate: number;
  totalBill: number;
  dueDate: string;
  penalty: number;
  status: string;
};

export default function Utilities() {
  const [utilities, setUtilities] = useState<Utility[]>([]);
  const [loading, setLoading] = useState(true);
  const [userName, setUserName] = useState<string | null>(null);
  const [userEmail, setUserEmail] = useState<string | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const token = localStorage.getItem("token");
        if (!token) return alert("Please sign in again.");

        const protectedRes = await axios.get("http://localhost:5238/protected", {
          headers: { Authorization: `Bearer ${token}` },
        });

        setUserName(protectedRes.data.name);
        setUserEmail(protectedRes.data.email);

        const utilityRes = await axios.get("http://localhost:5104/fetchUtilityBill", {
          headers: { Authorization: `Bearer ${token}` },
        });

        setUtilities(utilityRes.data);
      } catch (err: any) {
        console.error("Error fetching data:", err.response?.data || err.message);
        alert("Failed to fetch data.");
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  const handlePayment = async (u: Utility) => {
    try {
      // ⚠️ Only for testing: never use secret keys in frontend in production
      const secretKey = "sk_test_51SA19vAEfjVF6BcoEfogvOcdEAGNhySX1Ic2bv9IPB8XF51bEDuYjTuwWSRhiK0uokbzgcCiugN8ZcQ9fnS8BgH900sNICiu5P";

      const params = new URLSearchParams();
      params.append("payment_method_types[]", "card");
      params.append("line_items[0][price_data][currency]", "usd");
      params.append("line_items[0][price_data][product_data][name]", `Utility Bill #${u.utilityId}`);
      params.append("line_items[0][price_data][unit_amount]", Math.round(u.totalBill * 100).toString()); // cents
      params.append("line_items[0][quantity]", "1");
      params.append("mode", "payment");
      params.append("success_url", "http://localhost:5173/success");
      params.append("cancel_url", "http://localhost:5173/cancel");

      const response = await axios.post("https://api.stripe.com/v1/checkout/sessions", params, {
        headers: {
          "Authorization": `Bearer ${secretKey}`,
          "Content-Type": "application/x-www-form-urlencoded",
        },
      });
      
      localStorage.setItem("utilityId", `${u.utilityId}`);
      localStorage.setItem("gasUsage", `${u.gasUsage}`);
      localStorage.setItem("gasRate", `${u.gasRate}`);
      localStorage.setItem("waterUsage", `${u.waterUsage}`);
      localStorage.setItem("waterRate", `${u.waterRate}`);
      localStorage.setItem("electricityUsage", `${u.electricityUsage}`);
      localStorage.setItem("electricityRate", `${u.electricityRate}`);
      localStorage.setItem("totalBill", `${u.totalBill}`);
      localStorage.setItem("dueDate", u.dueDate);
      localStorage.setItem("penalty", `${u.penalty}`);
      localStorage.setItem("status", u.status);
        setTimeout(() => {
          // redirect after wait
          window.location.href = response.data.url;
        }, 100);

    } catch (err) {
      console.error("Stripe checkout error:", err);
      alert("Failed to initiate payment.");
    }
  };

  return (
    <Box sx={{ p: 4, display: "flex", flexDirection: "column", alignItems: "center", width: "100%", minHeight: "100vh" }}>
      <Typography variant="h3" mb={1}>Utilities Bill Payment</Typography>

      {userName && (
        <Typography variant="h6" mb={3}>
          Name: <strong>{userName}</strong> <br />
          Email: <strong>{userEmail}</strong>
        </Typography>
      )}

      {loading ? (
        <Typography>Loading...</Typography>
      ) : utilities.length === 0 ? (
        <Typography>No bills found.</Typography>
      ) : (
        <TableContainer component={Paper} sx={{ maxWidth: 1000 }}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Utility ID</TableCell>
                <TableCell>Gas</TableCell>
                <TableCell>Water</TableCell>
                <TableCell>Electricity</TableCell>
                <TableCell>Total Bill ($)</TableCell>
                <TableCell>Due Date</TableCell>
                <TableCell>Status</TableCell>
              </TableRow>
            </TableHead>

            <TableBody>
              {utilities.map((u) => (
                <TableRow key={u.utilityId}>
                  <TableCell>{u.utilityId}</TableCell>
                  <TableCell>{u.gasUsage} × ${u.gasRate.toFixed(2)} = ${ (u.gasUsage * u.gasRate).toFixed(2)}</TableCell>
                  <TableCell>{u.waterUsage} × ${u.waterRate.toFixed(2)} = ${ (u.waterUsage * u.waterRate).toFixed(2)}</TableCell>
                  <TableCell>{u.electricityUsage} × ${u.electricityRate.toFixed(2)} = ${ (u.electricityUsage * u.electricityRate).toFixed(2)}</TableCell>
                  <TableCell>${u.totalBill.toFixed(2)}</TableCell>
                  <TableCell>{new Date(u.dueDate).toLocaleDateString()}</TableCell>
                  <TableCell>
                    {u.status === "paid" ? (
                      <CheckCircleIcon color="success" />
                    ) : (
                      <Link component="button" underline="hover" color="primary" onClick={() => handlePayment(u)}>
                        Pay Now
                      </Link>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Box>
  );
}
