// PassBuyApply.tsx — Application-only page (no sign up / sign in UI)
// Updated: replaced 'Concession' tab with 'Youth' and 'Pensioner' tabs.
// Each calls the same Concession API with different CardType values.

import { useState } from "react";
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
} from "@mui/material";

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

export default function PassBuyApply() {
  // Which card type the user wants to apply for
  const [cardType, setCardType] = useState<"standard" | "education" | "transport" | "youth" | "pensioner">("standard");

  // toast
  const [snack, setSnack] = useState({ open: false, msg: "", severity: "success" as "success" | "error" | "info" });
  const openSnack = (msg: string, severity: "success" | "error" | "info" = "success") => setSnack({ open: true, msg, severity });

  // forms for each type
  const [edu, setEdu] = useState({ eduCode: "", stuNum: "", courseCode: "", courseTitle: "" });
  const [transport, setTransport] = useState({ employer: "", employeeNumber: "" });
  const [person, setPerson] = useState({ fullLegalName: "", DoB: formatIsoDate(new Date()) });

  const token = localStorage.getItem("token");
  const notAuthed = !token;

  // ---- submitters ----
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

  async function submitCard(path: string, params: any) {
    if (notAuthed) {
      openSnack("Please sign in first.", "info");
      return;
    }
    try {
      await api.post(path, null, { params, headers: getAuthHeaders() });
      openSnack("Application submitted.", "success");
    } catch (e) {
      openSnack(describeError(e, "Application failed"), "error");
    }
  }

  return (
    <Box sx={{ p: 3, maxWidth: 1000, mx: "auto" }}>
      <Typography variant="h4" sx={{ mb: 1 }}>PassBuy — Apply for a Card</Typography>
      <Typography variant="body2" sx={{ mb: 2, opacity: 0.75 }}>Backend: <code>{API_URL}</code></Typography>

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
          <ToggleButtonGroup value={cardType} exclusive onChange={(_,v)=>v&&setCardType(v)} sx={{ mb: 2, flexWrap:"wrap" }}>
            <ToggleButton value="standard">Standard</ToggleButton>
            <ToggleButton value="education">Education</ToggleButton>
            <ToggleButton value="transport">Transport</ToggleButton>
            <ToggleButton value="youth">Youth</ToggleButton>
            <ToggleButton value="pensioner">Pensioner</ToggleButton>
          </ToggleButtonGroup>

          <Divider sx={{ my: 2 }} />

          {cardType === "standard" && (
            <Box>
              <Typography variant="body2" sx={{ mb: 2 }}>Apply for a standard PassBuy card.</Typography>
              <Button variant="contained" disabled={notAuthed} onClick={applyStandard}>Apply — Standard</Button>
            </Box>
          )}

          {cardType === "education" && (
            <Grid container spacing={2}>
              <Grid item xs={12} md={6}><TextField label="Education code (eduCode)" fullWidth value={edu.eduCode} onChange={e=>setEdu({...edu,eduCode:e.target.value})} /></Grid>
              <Grid item xs={12} md={6}><TextField label="Student number" type="number" fullWidth value={edu.stuNum} onChange={e=>setEdu({...edu,stuNum:e.target.value})} /></Grid>
              <Grid item xs={12} md={6}><TextField label="Course code (number)" type="number" fullWidth value={edu.courseCode} onChange={e=>setEdu({...edu,courseCode:e.target.value})} /></Grid>
              <Grid item xs={12} md={6}><TextField label="Course title" fullWidth value={edu.courseTitle} onChange={e=>setEdu({...edu,courseTitle:e.target.value})} /></Grid>
              <Grid item xs={12}><Button variant="contained" disabled={notAuthed} onClick={applyEducation}>Apply — Education</Button></Grid>
            </Grid>
          )}

          {cardType === "transport" && (
            <Grid container spacing={2}>
              <Grid item xs={12} md={6}><TextField label="Employer" fullWidth value={transport.employer} onChange={e=>setTransport({...transport,employer:e.target.value})} /></Grid>
              <Grid item xs={12} md={6}><TextField label="Employee number" type="number" fullWidth value={transport.employeeNumber} onChange={e=>setTransport({...transport,employeeNumber:e.target.value})} /></Grid>
              <Grid item xs={12}><Button variant="contained" disabled={notAuthed} onClick={applyTransport}>Apply — Transport Employee</Button></Grid>
            </Grid>
          )}

          {(cardType === "youth" || cardType === "pensioner") && (
            <Grid container spacing={2}>
              <Grid item xs={12} md={6}><TextField label="Full legal name" fullWidth value={person.fullLegalName} onChange={e=>setPerson({...person,fullLegalName:e.target.value})} /></Grid>
              <Grid item xs={12} md={6}><TextField label="Date of birth" type="date" fullWidth InputLabelProps={{ shrink: true }} value={person.DoB} onChange={e=>setPerson({...person,DoB:e.target.value})} /></Grid>
              <Grid item xs={12}><Button variant="contained" disabled={notAuthed} onClick={()=>applyConcession(cardType === "youth" ? 2 : 3)}>
                Apply — {cardType === "youth" ? "Youth Concession" : "Pensioner Concession"}
              </Button></Grid>
            </Grid>
          )}
        </CardContent>
      </Card>

      <Snackbar open={snack.open} autoHideDuration={3500} onClose={()=>setSnack(s=>({...s,open:false}))}>
        <Alert onClose={()=>setSnack(s=>({...s,open:false}))} severity={snack.severity} variant="filled" sx={{ width: '100%' }}>
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
