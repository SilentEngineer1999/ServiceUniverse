// PassBuyCards.tsx — List all cards for the authenticated user (port 5101)
// Assumes JWT is in localStorage("token").
// On load: POST /PassBuy/applications/stale to clean up pending applications.

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
  Chip,
  Tooltip,
  Link,
  Alert,
  Button,
  Stack,
} from "@mui/material";
import CreditCardIcon from "@mui/icons-material/CreditCard";
import AddCircleOutlineIcon from "@mui/icons-material/AddCircleOutline";

const API_URL = "http://localhost:5101";
const api = axios.create({ baseURL: API_URL });

function authHeaders() {
  const token = localStorage.getItem("token");
  return token ? { Authorization: `Bearer ${token}` } : {};
}

type CardType =
  | "Standard"
  | "EducationConcession"
  | "YouthConcession"
  | "PensionerConcession"
  | "TransportEmployeeConcession";

type PassBuyCardDto = {
  id: number;
  userId: string;
  cardType: CardType | string;
  dateApproved: string;
  topUpMode: string; // manual | auto | scheduled
  autoThreshold?: number | null;
  topUpAmount?: number | null;
  topUpSchedule?: string | null; // weekly | monthly
  bankAccount: string;
};

export default function PassBuyCards() {
  const [cards, setCards] = useState<PassBuyCardDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    try {
      const token = localStorage.getItem("token");
      if (!token) {
        setError("Please sign in again.");
        setLoading(false);
        return;
      }

      // Sneaky cleanup: use POST to avoid some DELETE preflight/proxy issues.
      (async () => {
        try {
          console.log("Cleaning up stale applications...");
          const resp = await api.post(
            "/PassBuy/applications/stale",
            null,
            { headers: authHeaders(), timeout: 8000, validateStatus: () => true }
          );
          console.log("Cleanup response:", resp.status, resp.data);
        } catch (err: any) {
          console.error("Cleanup failed:", err?.message || err);
        }
      })();

      const res = await api.get("/PassBuy/cards", { headers: authHeaders() });
      setCards(res.data as PassBuyCardDto[]);
    } catch (err: any) {
      console.error("Error fetching cards:", err.response?.data || err.message);
      setError("Failed to fetch cards.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  return (
    <Box
      sx={{
        p: 4,
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        width: "100%",
        minHeight: "100vh",
      }}
    >
      <Stack
        direction="row"
        alignItems="center"
        justifyContent="space-between"
        sx={{ width: "100%", maxWidth: 1100, mb: 2 }}
      >
        <Typography variant="h3" sx={{ display: "flex", alignItems: "center", gap: 1 }}>
          <CreditCardIcon /> Your PassBuy Cards
        </Typography>

        <Button
          variant="contained"
          href="/PassBuy/apply"
          sx={{
            backgroundColor: "#8bc34a",
            "&:hover": { backgroundColor: "#7cb342" },
            minWidth: 48,
            width: 48,
            height: 48,
            borderRadius: "50%",
            p: 0,
          }}
          aria-label="Create new card"
          title="Create new card"
        >
          <AddCircleOutlineIcon sx={{ color: "white", fontSize: 28 }} />
        </Button>
      </Stack>

      {loading && <Typography>Loading...</Typography>}

      {!loading && error && (
        <Alert severity="error" sx={{ maxWidth: 1000, width: "100%", mb: 2 }}>
          {error}
        </Alert>
      )}

      {!loading && !error && cards.length === 0 && (
        <Paper variant="outlined" sx={{ p: 3, maxWidth: 800, textAlign: "center" }}>
          <Typography variant="h6" gutterBottom>
            No cards yet
          </Typography>
          <Typography variant="body2" gutterBottom>
            Apply for a card to see it here.
          </Typography>
          <Link href="/PassBuy/apply">Go to application page</Link>
        </Paper>
      )}

      {!loading && !error && cards.length > 0 && (
        <TableContainer component={Paper} sx={{ maxWidth: 1100 }}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>ID</TableCell>
                <TableCell>Type</TableCell>
                <TableCell>Approved</TableCell>
                <TableCell>Top-up</TableCell>
                <TableCell>Amount / Threshold</TableCell>
                <TableCell>Schedule</TableCell>
                <TableCell>Bank</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {cards.map((c) => {
                const approvedDate = new Date(c.dateApproved);
                const mode = (c.topUpMode || "manual").toLowerCase();
                const modeLabel = mode === "auto" ? "Auto" : mode === "scheduled" ? "Scheduled" : "Manual";
                return (
                  <TableRow key={c.id} hover>
                    <TableCell>{c.id}</TableCell>
                    <TableCell>{c.cardType}</TableCell>
                    <TableCell>{approvedDate.toLocaleString()}</TableCell>
                    <TableCell>
                      <Chip size="small" label={modeLabel} />
                    </TableCell>
                    <TableCell>
                      <Tooltip title="Top-up amount (and auto threshold if applicable)">
                        <span>
                          {c.topUpAmount != null ? `$${c.topUpAmount}` : "—"}
                          {c.autoThreshold != null ? ` / thr $${c.autoThreshold}` : ""}
                        </span>
                      </Tooltip>
                    </TableCell>
                    <TableCell>{c.topUpSchedule ?? "—"}</TableCell>
                    <TableCell>{c.bankAccount}</TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Box>
  );
}
