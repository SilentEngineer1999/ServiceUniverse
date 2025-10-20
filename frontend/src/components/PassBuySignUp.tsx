import { useState } from "react";
import axios from "axios";
import { Box, Typography, TextField, Button } from "@mui/material";
import { useNavigate } from "react-router-dom";

type SignUpProps = {
  onclick: () => void;
};

export default function SignUp({ onclick }: SignUpProps) {
  const [fname, setFname] = useState("");
  const [lname, setLname] = useState("");
  const [email, setEmail] = useState("");
  const [age, setAge] = useState("");
  const [password, setPassword] = useState("");
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!fname || !lname || !email || !age || !password) {
      alert("Please fill all fields");
      return;
    }

    try {
      const response = await axios.post("http://localhost:5101/signUp", null, {
        params: {
          fname,
          lname,
          age: Number(age),
          email,
          password,
        },
      });

      console.log("Sign up success:", response.data);
      localStorage.setItem("token", response.data.token);
      navigate("/utilities");
    } catch (error: any) {
      console.error("Sign up failed:", error.response?.data || error.message);
      alert("Failed to sign up. Please try again.");
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
        Sign Up
      </Typography>

      <TextField
        label="First Name"
        type="text"
        variant="outlined"
        fullWidth
        value={fname}
        onChange={(e) => setFname(e.target.value)}
      />

      <TextField
        label="Last Name"
        type="text"
        variant="outlined"
        fullWidth
        value={lname}
        onChange={(e) => setLname(e.target.value)}
      />

      <TextField
        label="Email"
        type="email"
        variant="outlined"
        fullWidth
        value={email}
        onChange={(e) => setEmail(e.target.value)}
      />

      <TextField
        label="Age"
        type="number"
        variant="outlined"
        fullWidth
        value={age}
        onChange={(e) => setAge(e.target.value)}
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
        Already have an account?
        <Button
          sx={{
            ml: 1,
            textTransform: "none",
            "&:focus": { outline: "none" },
          }}
          onClick={onclick}
        >
          Sign In
        </Button>
      </Typography>

      <Button variant="contained" color="primary" type="submit" fullWidth>
        Sign Up
      </Button>
    </Box>
  );
}
