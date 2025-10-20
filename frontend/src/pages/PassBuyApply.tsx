// PassBuyApply.tsx — Application-only page (no sign up / sign in UI)
// Uses dropdowns for Universities and Transport Employers.
// Port: 5101

import { useEffect, useState } from "react";
import axios, { AxiosError } from "axios";
import {
  Box,
  Button,
  Card,
  CardContent,
  Divider,
  Grid,
  TextField,
  Typography,
  Snackbar,
  Alert,
  Paper,
  ToggleButton,
  ToggleButtonGroup,
  Link as MLink,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
} from "@mui/material";
import { useNavigate } from "react-router-dom";

const API_URL = "http://localhost:5101";
const api = axios.create({ baseURL: API_URL });

function getAuthHeaders() {
  const token = localStorage.getItem("token");
  return token ? { Authorization: `Bearer ${token}` } : {};
}

function formatIsoDate(d: string | Date): string {
  const date = typeof d === "string" ? new Date(d) : d;
  return date.toISOString().slice(0, 10);
}

type EducationProvider = { id: number; name: string; eduCode: string };
type TransportEmployer = { id: number; name: string };

export default function PassBuyApply() {
  const navigate = useNavigate();

  const [cardType, setCardType] = useState<"standard" | "education" | "transport" | "youth" | "pensioner">("standard");

  const [snack, setSnack] = useState({ open: false, msg: "", severity: "success" as "success" | "error" | "info" });
  const openSnack = (msg: string, severity: "success" | "error" | "info" = "success") =>
    setSnack({ open: true, msg, severity });

  // dropdown data
  const [providers, setProviders] = useState<EducationProvider[]>([]);
  const [employers, setEmployers] = useState<TransportEmployer[]>([]);

  // forms
  const [edu, setEdu] = useState({ eduCode: "", stuNum: "", courseCode: "", courseTitle: "" });
  const [transport, setTransport] = useState({ employer: "", employeeNumber: "" });
  const [person, setPerson] = useState({ fullLegalName: "", DoB: formatIsoDate(new Date()) });

  const token = localStorage.getItem("token");
  const notAuthed = !token;

  useEffect(() => {
    (async () => {
      try {
        const [p, t] = await Promise.all([
          api.get("/PassBuy/educationProviders", { headers: getAuthHeaders() }),
          api.get("/PassBuy/transportEmployers", { headers: getAuthHeaders() }),
        ]);
        setProviders(p.data as EducationProvider[]);
        setEmployers(t.data as TransportEmployer[]);
      } catch {
        // fallback: empty (page still works, just no dropdown options)
        setProviders([]);
        setEmployers([]);
      }
    })();
  }, []);

  async function submitCard(path: string, params: any) {
    if (notAuthed) {
      openSnack("Please sign in first.", "info");
      return;
    }
    try {
      await api.post(path, null, { params, headers: getAuthHeaders() });
      openSnack("Application submitted.", "success");
      setTimeout(() => navigate("/PassBuy/fulfillment"), 1200);
    } catch (e) {
      openSnack(describeError(e, "Application failed"), "error");
    }
  }

  async function applyStandard() {
    await submitCard("/PassBuy/newCard/standard", {});
  }

  async function applyEducation() {
    const params = {
      eduCode: edu.eduCode,
      stuNum: parseInt(edu.stuNum || "0", 10),
      courseCode: parseInt(edu.courseCode || "0", 10),
      courseTitle: edu.courseTitle,
    };
    await submitCard("/PassBuy/newCard/education", params);
  }

  async function applyTransport() {
    const params = {
      employer: transport.employer,
      employeeNumber: parseInt(transport.employeeNumber || "0", 10),
    };
    await submitCard("/PassBuy/newCard/transportEmployee", params);
  }

  async function applyConcession(cardTypeValue: number) {
    const params = {
      DoB: person.DoB,
      fullLegalName: person.fullLegalName,
      cardType: cardTypeValue,
    };
    await submitCard("/PassBuy/newCard/concession", params);
  }

  return (
    <Box sx={{ p: 3, maxWidth: 1000, mx: "auto" }}>
      <Typography variant="h4" sx={{ mb: 1 }}>PassBuy — Apply for a Card</Typography>
      <Typography variant="body2" sx={{ mb: 2, opacity: 0.75 }}>
        Backend: <code>{API_URL}</code>
      </Typography>

      {notAuthed && (
        <Paper variant="outlined" sx={{ p: 2, mb: 3 }}>
          <Typography sx={{ mb: 0.5 }}>You are not signed in.</Typography>
          <Typography variant="body2">
            Go to your <MLink href="/signin">Sign In</MLink> page, then return here to submit your application.
          </Typography>
        </Paper>
      )}

      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>Choose a card type</Typography>
          <ToggleButtonGroup
            value={cardType}
            exclusive
            onChange={(_, v) => v && setCardType(v)}
            sx={{ mb: 2, flexWrap: "wrap" }}
          >
            <ToggleButton value="standard">Standard</ToggleButton>
            <ToggleButton value="education">Education</ToggleButton>
            <ToggleButton value="transport">Transport</ToggleButton>
            <ToggleButton value="youth">Youth</ToggleButton>
            <ToggleButton value="pensioner">Pensioner</ToggleButton>
          </ToggleButtonGroup>

          <Divider sx={{ my: 2 }} />

          {cardType === "standard" && (
            <Box>
              <Typography variant="body2" sx={{ mb: 2 }}>
                Apply for a standard PassBuy card.
              </Typography>
              <Button variant="contained" disabled={notAuthed} onClick={applyStandard}>
                Apply — Standard
              </Button>
            </Box>
          )}

          {cardType === "education" && (
            <Grid container spacing={2}>
              <Grid item xs={12} md={6}>
                <FormControl fullWidth>
                  <InputLabel id="provider-label">University</InputLabel>
                  <Select
                    labelId="provider-label"
                    label="University"
                    value={edu.eduCode}
                    onChange={(e) => setEdu({ ...edu, eduCode: e.target.value as string })}
                  >
                    {providers.map((p) => (
                      <MenuItem key={p.id} value={p.eduCode}>
                        {p.name} ({p.eduCode})
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12} md={6}>
                <TextField
                  label="Student number"
                  type="number"
                  fullWidth
                  value={edu.stuNum}
                  onChange={(e) => setEdu({ ...edu, stuNum: e.target.value })}
                />
              </Grid>
              <Grid item xs={12} md={6}>
                <TextField
                  label="Course code (number)"
                  type="number"
                  fullWidth
                  value={edu.courseCode}
                  onChange={(e) => setEdu({ ...edu, courseCode: e.target.value })}
                />
              </Grid>
              <Grid item xs={12} md={6}>
                <TextField
                  label="Course title"
                  fullWidth
                  value={edu.courseTitle}
                  onChange={(e) => setEdu({ ...edu, courseTitle: e.target.value })}
                />
              </Grid>
              <Grid item xs={12}>
                <Button variant="contained" disabled={notAuthed} onClick={applyEducation}>
                  Apply — Education
                </Button>
              </Grid>
            </Grid>
          )}

          {cardType === "transport" && (
            <Grid container spacing={2}>
              <Grid item xs={12} md={6}>
                <FormControl fullWidth>
                  <InputLabel id="employer-label">Transport employer</InputLabel>
                  <Select
                    labelId="employer-label"
                    label="Transport employer"
                    value={transport.employer}
                    onChange={(e) => setTransport({ ...transport, employer: e.target.value as string })}
                  >
                    {employers.map((t) => (
                      <MenuItem key={t.id} value={t.name}>
                        {t.name}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12} md={6}>
                <TextField
                  label="Employee number"
                  type="number"
                  fullWidth
                  value={transport.employeeNumber}
                  onChange={(e) => setTransport({ ...transport, employeeNumber: e.target.value })}
                />
              </Grid>
              <Grid item xs={12}>
                <Button variant="contained" disabled={notAuthed} onClick={applyTransport}>
                  Apply — Transport Employee
                </Button>
              </Grid>
            </Grid>
          )}

          {(cardType === "youth" || cardType === "pensioner") && (
            <Grid container spacing={2}>
              <Grid item xs={12} md={6}>
                <TextField
                  label="Full legal name"
                  fullWidth
                  value={person.fullLegalName}
                  onChange={(e) => setPerson({ ...person, fullLegalName: e.target.value })}
                />
              </Grid>
              <Grid item xs={12} md={6}>
                <TextField
                  label="Date of birth"
                  type="date"
                  fullWidth
                  InputLabelProps={{ shrink: true }}
                  value={person.DoB}
                  onChange={(e) => setPerson({ ...person, DoB: e.target.value })}
                />
              </Grid>
              <Grid item xs={12}>
                <Button
                  variant="contained"
                  disabled={notAuthed}
                  onClick={() => applyConcession(cardType === "youth" ? 2 : 3)}
                >
                  Apply — {cardType === "youth" ? "Youth Concession" : "Pensioner Concession"}
                </Button>
              </Grid>
            </Grid>
          )}
        </CardContent>
      </Card>

      <Snackbar
        open={snack.open}
        autoHideDuration={3500}
        onClose={() => setSnack((s) => ({ ...s, open: false }))}
      >
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

function describeError(err: unknown, prefix = "Error"): string {
  const e = err as AxiosError<any>;
  const detail = e.response?.data?.message || e.response?.data || e.message || String(err);
  return `${prefix}: ${detail}`;
}
