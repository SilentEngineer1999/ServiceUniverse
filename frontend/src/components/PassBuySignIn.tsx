import { useState } from "react";
import axios from "axios";
import { Box, Typography, TextField, Button } from "@mui/material";
import { useNavigate } from "react-router-dom";

type SignInProps = {
  onclick: () => void;
};


export default function SignIn({ onclick }: SignInProps) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      // ✅ Send as query params since .NET expects them as (string email, string password)
      const response = await axios.post(
        `http://localhost:5101/PassBuy/signIn?email=${encodeURIComponent(email)}&password=${encodeURIComponent(password)}`
      );

      console.log("Sign in success:", response.data);
      localStorage.setItem("token", response.data.token);
      navigate("/PassBuy/apply");
    } catch (error: any) {
      console.error("Sign in failed:", error.response?.data || error.message);
      alert("Invalid credentials or server error");
    }
  };

  return (
    <Box
      component="form"
      onSubmit={handleSubmit}
      sx={{
        display: "flex",
        flexDirection: "column",
        gap: 2,
        width: "100%",
        maxWidth: 400,
      }}
    >
      <Typography variant="h4" mb={2}>
        Sign In
      </Typography>

      <TextField
        label="Email"
        type="email"
        variant="outlined"
        fullWidth
        value={email}
        onChange={(e) => setEmail(e.target.value)}
      />
      <TextField
        label="Password"
        type="password"
        variant="outlined"
        fullWidth
        value={password}
        onChange={(e) => setPassword(e.target.value)}
      />

      <Typography>
        Don't have an account?
        <Button
          variant="text"
          disableRipple
          disableFocusRipple
          sx={{ ml: 1, textTransform: "none", "&:focus": { outline: "none" } }}
          onClick={onclick}
        >
          Sign Up
        </Button>
      </Typography>

      <Button variant="contained" color="primary" type="submit" fullWidth>
        Sign In
      </Button>
    </Box>
  );
}
