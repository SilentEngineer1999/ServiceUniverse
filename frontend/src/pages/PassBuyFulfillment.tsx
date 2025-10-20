// PassBuyFulfilment.tsx — Address & Funding page (port 5101)
// Collects mailing address, top-up preference, and bank account selection.
// Separate page/component. Assumes JWT is in localStorage("token").

import { useEffect, useMemo, useState } from "react";
import axios, { AxiosError } from "axios";
import {
  Box,
  Button,
  Card,
  CardContent,
  Grid,
  TextField,
  Typography,
  Snackbar,
  Alert,
  RadioGroup,
  FormControlLabel,
  Radio,
  FormControl,
  FormLabel,
  Select,
  MenuItem,
  InputLabel,
  Paper,
  Link as MLink,
} from "@mui/material";
import { useNavigate } from "react-router-dom";

const API_URL = "http://localhost:5101";
const api = axios.create({ baseURL: API_URL });

function authHeaders() {
  const token = localStorage.getItem("token");
  return token ? { Authorization: `Bearer ${token}` } : {};
}

type BankAccount = { id: string; displayName: string; maskedIban?: string };

type TopUpMode = "manual" | "auto" | "scheduled";

export default function PassBuyFulfilment() {
  const navigate = useNavigate();

  const [snack, setSnack] = useState<{ open: boolean; msg: string; severity: "success" | "error" | "info" }>({
    open: false,
    msg: "",
    severity: "success",
  });
  const openSnack = (msg: string, severity: "success" | "error" | "info" = "success") =>
    setSnack({ open: true, msg, severity });

  const token = useMemo(() => localStorage.getItem("token"), []);
  const notAuthed = !token;

  // address (single line)
  const [addr, setAddr] = useState({
    address: "",
    city: "",
    state: "",
    postcode: "",
    country: "Australia",
  });

  // top-up
  const [mode, setMode] = useState<TopUpMode>("manual");
  const [auto, setAuto] = useState({ threshold: "25", amount: "20" });
  const [sched, setSched] = useState({ cadence: "weekly", amount: "20" }); // weekly | monthly

  // bank
  const [bankAccounts, setBankAccounts] = useState<BankAccount[]>([]);
  const [bankId, setBankId] = useState<string>("");

  useEffect(() => {
    async function loadBankAccounts() {
      if (notAuthed) return;
      try {
        const res = await api.get("/PassBuy/bankAccounts", { headers: authHeaders() });
        setBankAccounts(res.data as BankAccount[]);
      } catch {
        setBankAccounts([
          { id: "demo-001", displayName: "Everyday Account ••1234" },
          { id: "demo-002", displayName: "Savings Account ••9876" },
        ]);
      }
    }
    loadBankAccounts();
  }, [notAuthed]);

  function requiredOK(): boolean {
    if (!addr.address || !addr.city || !addr.state || !addr.postcode || !addr.country) return false;
    if (!bankId) return false;
    if (mode === "auto") return !!auto.threshold && !!auto.amount;
    if (mode === "scheduled") return !!sched.cadence && !!sched.amount;
    return true;
  }

  async function handleSubmit() {
    if (notAuthed) {
      openSnack("Please sign in first.", "info");
      return;
    }

    const params: any = {
      address: addr.address,
      city: addr.city,
      state: addr.state,
      postcode: addr.postcode,
      country: addr.country,
      bankAccountId: bankId,
      topupMode: mode,
    };

    if (mode === "auto") {
      params.autoThreshold = parseFloat(auto.threshold || "0");
      params.autoAmount = parseFloat(auto.amount || "0");
    } else if (mode === "scheduled") {
      params.scheduleCadence = sched.cadence;
      params.scheduleAmount = parseFloat(sched.amount || "0");
    }

    try {
      await api.post("/PassBuy/fulfilment", null, { params, headers: authHeaders() });
      openSnack("Details saved.", "success");
      setTimeout(() => {
        navigate("/PassBuy/cards");
      }, 2000);
    } catch (e) {
      openSnack(describeErr(e, "Failed to save"), "error");
    }
  }

  return (
    <Box sx={{ p: 3, maxWidth: 900, mx: "auto" }}>
      <Typography variant="h4" sx={{ mb: 2 }}>
        PassBuy — Delivery & Funding
      </Typography>

      {notAuthed && (
        <Paper variant="outlined" sx={{ p: 2, mb: 3 }}>
          <Typography sx={{ mb: 0.5 }}>You are not signed in.</Typography>
          <Typography variant="body2">
            Go to your <MLink href="/signin">Sign In</MLink> page, then return here to continue.
          </Typography>
        </Paper>
      )}

      {/* Address */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Mailing address
          </Typography>
          <Grid container spacing={2}>
            <Grid item xs={12}>
              <TextField
                label="Address"
                fullWidth
                value={addr.address}
                onChange={(e) => setAddr((a) => ({ ...a, address: e.target.value }))}
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                label="City / Suburb"
                fullWidth
                value={addr.city}
                onChange={(e) => setAddr((a) => ({ ...a, city: e.target.value }))}
              />
            </Grid>
            <Grid item xs={12} md={3}>
              <TextField
                label="State/Territory"
                fullWidth
                value={addr.state}
                onChange={(e) => setAddr((a) => ({ ...a, state: e.target.value }))}
              />
            </Grid>
            <Grid item xs={12} md={3}>
              <TextField
                label="Postcode"
                fullWidth
                value={addr.postcode}
                onChange={(e) => setAddr((a) => ({ ...a, postcode: e.target.value }))}
              />
            </Grid>
            <Grid item xs={12} md={4}>
              <TextField
                label="Country"
                fullWidth
                value={addr.country}
                onChange={(e) => setAddr((a) => ({ ...a, country: e.target.value }))}
              />
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* Top-up preference */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Top-up preference
          </Typography>
          <FormControl component="fieldset" sx={{ mb: 2 }}>
            <FormLabel>Mode</FormLabel>
            <RadioGroup row value={mode} onChange={(_, v) => setMode(v as TopUpMode)}>
              <FormControlLabel value="manual" control={<Radio />} label="Manual (I’ll top up myself)" />
              <FormControlLabel value="auto" control={<Radio />} label="Auto top-up" />
              <FormControlLabel value="scheduled" control={<Radio />} label="Scheduled" />
            </RadioGroup>
          </FormControl>

          {mode === "auto" && (
            <Grid container spacing={2}>
              <Grid item xs={12} md={6}>
                <TextField
                  label="Auto top-up threshold ($)"
                  type="number"
                  fullWidth
                  value={auto.threshold}
                  onChange={(e) => setAuto((s) => ({ ...s, threshold: e.target.value }))}
                />
              </Grid>
              <Grid item xs={12} md={6}>
                <TextField
                  label="Auto top-up amount ($)"
                  type="number"
                  fullWidth
                  value={auto.amount}
                  onChange={(e) => setAuto((s) => ({ ...s, amount: e.target.value }))}
                />
              </Grid>
            </Grid>
          )}

          {mode === "scheduled" && (
            <Grid container spacing={2}>
              <Grid item xs={12} md={6}>
                <FormControl fullWidth>
                  <InputLabel id="cadence-label">Cadence</InputLabel>
                  <Select
                    labelId="cadence-label"
                    label="Cadence"
                    value={sched.cadence}
                    onChange={(e) => setSched((s) => ({ ...s, cadence: e.target.value as string }))}
                  >
                    <MenuItem value="weekly">Weekly</MenuItem>
                    <MenuItem value="monthly">Monthly</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12} md={6}>
                <TextField
                  label="Scheduled amount ($)"
                  type="number"
                  fullWidth
                  value={sched.amount}
                  onChange={(e) => setSched((s) => ({ ...s, amount: e.target.value }))}
                />
              </Grid>
            </Grid>
          )}
        </CardContent>
      </Card>

      {/* Bank account */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Bank account
          </Typography>
          <FormControl fullWidth>
            <InputLabel id="bank-label">Choose account</InputLabel>
            <Select
              labelId="bank-label"
              label="Choose account"
              value={bankId}
              onChange={(e) => setBankId(e.target.value as string)}
            >
              {bankAccounts.map((b) => (
                <MenuItem key={b.id} value={b.id}>
                  {b.displayName}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </CardContent>
      </Card>

      <Box sx={{ display: "flex", gap: 2 }}>
        <Button variant="contained" disabled={!requiredOK() || notAuthed} onClick={handleSubmit}>
          Save & Continue
        </Button>
        <Button variant="text" onClick={() => navigate(-1)}>
          Back
        </Button>
      </Box>

      <Snackbar open={snack.open} autoHideDuration={3500} onClose={() => setSnack((s) => ({ ...s, open: false }))}>
        <Alert
          onClose={() => setSnack((s) => ({ ...s, open: false }))}
          severity={snack.severity}
          variant="filled"
          sx={{ width: "100%" }}
        >
          {snack.msg}
        </Alert>
      </Snackbar>
    </Box>
  );
}

function describeErr(err: unknown, prefix: string): string {
  const e = err as AxiosError<any>;
  const detail = e.response?.data?.message || e.response?.data || e.message || String(err);
  return `${prefix}: ${detail}`;
}
