import { Box, Grid, Typography } from "@mui/material";
import ServiceCard from "../components/ServiceCard";
import TungstenIcon from "@mui/icons-material/Tungsten";

export default function Home() {
  return (
    <Box style={{position: 'absolute', top:0, left: 0, marginLeft: 15}}>
      <Typography variant="h1">Service Universe</Typography>
      <Typography variant="h4">Welcome to Service Universe a portal to manage all your government services. Select a service below.</Typography>
      <Grid container spacing={2} mt={2}>
        <Grid size={3}>
            <nav><a href="/auth"><ServiceCard text={"Utility Payment Service"} color={"green"} Icon={TungstenIcon}/></a></nav>
        </Grid>
        <Grid size={3}>
          <ServiceCard text={"Health Service"} color={""} />
        </Grid>
        <Grid size={3}>
            <nav><a href="/PassBuy/auth"><ServiceCard text={"PassBuy Service"} color={"red"} Icon={TungstenIcon}/></a></nav>
        </Grid>
      </Grid>
    </Box>
  );
}