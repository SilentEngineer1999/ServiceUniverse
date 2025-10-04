import { Box, Button, Typography } from "@mui/material";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import { useEffect, useState } from "react";
import axios from "axios";
import { useNavigate } from "react-router-dom";

export default function Success() {
  const navigate = useNavigate();
  const [bill, setBill] = useState<{
    utilityId: number;
    gasUsage: number;
    gasRate: number;
    waterUsage: number;
    waterRate: number;
    electricityUsage: number;
    electricityRate: number;
    totalBill: number;
  } | null>(null);

  useEffect(() => {
    const storedUtilityId = parseInt(localStorage.getItem("utilityId") || "0");
    const gasUsage = parseFloat(localStorage.getItem("gasUsage") || "0");
    const gasRate = parseFloat(localStorage.getItem("gasRate") || "0");
    const waterUsage = parseFloat(localStorage.getItem("waterUsage") || "0");
    const waterRate = parseFloat(localStorage.getItem("waterRate") || "0");
    const electricityUsage = parseFloat(localStorage.getItem("electricityUsage") || "0");
    const electricityRate = parseFloat(localStorage.getItem("electricityRate") || "0");
    const totalBill = parseFloat(localStorage.getItem("totalBill") || "0");

    setBill({ utilityId: storedUtilityId, gasUsage, gasRate, waterUsage, waterRate, electricityUsage, electricityRate, totalBill });

    // Call backend to mark as paid with JWT token
    const confirmPayment = async () => {
      try {
        const token = localStorage.getItem("token"); // JWT token
        if (!token) {
          console.error("No token found, cannot confirm payment");
          return;
        }

        await axios.post(
          `http://localhost:5104/paymentConfirmed?utilityId=${storedUtilityId}`,
          {},
          {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }
        );

        console.log("Payment status updated to paid on backend");
      } catch (err) {
        console.error("Failed to confirm payment:", err);
      }
    };

    if (storedUtilityId) {
      confirmPayment();
    }

  }, []);

  if (!bill) return <Typography>Loading...</Typography>;

  const gasTotal = bill.gasUsage * bill.gasRate;
  const waterTotal = bill.waterUsage * bill.waterRate;
  const electricityTotal = bill.electricityUsage * bill.electricityRate;

  return (
    <Box style={{ padding: 20 }}>
      <Typography variant="h4" gutterBottom>
        Payment Successful! <CheckCircleIcon color="success" />
      </Typography>

      <Typography variant="h6" gutterBottom>
        Breakdown:
      </Typography>

      <Typography>{bill.gasUsage} * {bill.gasRate} = {gasTotal.toFixed(2)}</Typography>
      <Typography>{bill.waterUsage} * {bill.waterRate} = {waterTotal.toFixed(2)}</Typography>
      <Typography>{bill.electricityUsage} * {bill.electricityRate} = {electricityTotal.toFixed(2)}</Typography>
      
      <Typography variant="h6" style={{ marginTop: 10 }}>
        Total = <strong>${bill.totalBill.toFixed(2)}</strong> <CheckCircleIcon color="success" />
      </Typography>
      <Button variant="contained" color="success" onClick={()=>{navigate("/utilities")}}>Return to Payments</Button>
    </Box>
  );
}
