import { Box, Typography, TextField, Button, Divider } from "@mui/material";
import SignIn from "../components/PassBuySignIn";
import { useState } from "react";
import SignUp from "../components/PassBuySignUp";

export default function PassBuyAuth() {
  const [signIn, useSignIn] = useState(true);

  function signInformChange()
  {
    useSignIn(!signIn);
  }
  return (
    <Box
      sx={{
        display: "flex",
        height: "100vh", // full screen height
        width: "100%",
      }}
    >
      {/* Left Side */}
      <Box
        sx={{
          flex: 1,
          backgroundColor: "orange",
          color: "white",
          display: "flex",
          flexDirection: "column",
          justifyContent: "center",
          alignItems: "center",
          textAlign: "center",
          p: 4,
        }}
      >
        <Typography variant="h1" sx={{ fontSize: { xs: 40, md: 80 } }}>
          PassBuy Transport Service
        </Typography>
      </Box>
      <Box
        sx={{
          flex: 1,
          display: "flex",
          justifyContent: "center",
          alignItems: "center",
          p: 4,
        }}
      >
           {signIn ? <SignIn onclick={signInformChange}/> : <SignUp onclick={signInformChange} />}
      </Box>
    </Box>
  );
}
