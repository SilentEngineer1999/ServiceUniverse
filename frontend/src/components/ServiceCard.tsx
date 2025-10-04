import { Avatar, Card, Typography, Box } from "@mui/material";
import type { SvgIconComponent } from "@mui/icons-material"; // ✅ type-only import

type ServiceCardProps = {
  text: string;
  color: string;
  Icon?: SvgIconComponent; // optional MUI icon component
};

export default function ServiceCard({ text, color, Icon }: ServiceCardProps) {
  return (
    <Card
      variant="outlined"
      sx={{
        p: 2,
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        py: 10,
      }}
    >
      <Box display="flex" flexDirection="column" alignItems="center" gap={1}>
        <Avatar sx={{ bgcolor: color, width:60, height:60 }}>
          {Icon ? <Icon /> : null}
        </Avatar>
        <Typography variant="h6" fontWeight="bold">
          {text}
        </Typography>
      </Box>
    </Card>
  );
}
