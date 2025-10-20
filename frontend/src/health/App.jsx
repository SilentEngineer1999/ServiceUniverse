import React, { useEffect, useMemo, useState } from "react";

/**
 * =============================================
 * BACKEND API CLIENT (Auth + Doctors + Appointments)
 * =============================================
 * Set your backend base URL here or via Vite env:
 *   VITE_API_BASE=http://localhost:5102
 */
const API_BASE = import.meta.env.VITE_API_BASE || "http://localhost:5102";

async function http(path, { method = "GET", body, token } = {}) {
  const res = await fetch(`${API_BASE}${path}`, {
    method,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: body ? JSON.stringify(body) : undefined,
  });

  if (!res.ok) {
    const text = await res.text();
    try {
      const j = JSON.parse(text);
      throw new Error(j?.message || j?.error || text || `HTTP ${res.status}`);
    } catch {
      throw new Error(text || `HTTP ${res.status}`);
    }
  }
  const ct = res.headers.get("content-type") || "";
  if (ct.includes("application/json")) return res.json();
  const t = await res.text();
  try { return JSON.parse(t); } catch { return t; }
}

// -------- Auth --------
async function apiSignup({ name, email, password, role = "patient" }) {
  return http("/api/auth/signup", { method: "POST", body: { name, email, password, role } });
}
async function apiLogin({ email, password }) {
  return http("/api/auth/login", { method: "POST", body: { email, password } });
}

// -------- Doctors & Appointments --------
async function apiListDoctors() {
  // Expect: [{ id, name, specialty, slots: ["2025-10-16T09:00", ...] }]
  return http("/api/doctors");
}
async function apiListAppointments() {
  // Expect: [{ id, patientName, doctorId, doctorName, time }]
  return http("/api/appointments");
}
async function apiCreateAppointment({ token, patientName, doctorId, time }) {
  return http("/api/appointments", { method: "POST", token, body: { patientName, doctorId, time } });
}

// ---------------- UI Primitives ----------------
const Shell = ({ children }) => (
  <div style={{ minHeight: '100vh', color: '#111' }}>
    <div style={{ maxWidth: 1080, margin: '0 auto', padding: '24px' }}>{children}</div>
  </div>
);
const Card = ({ title, children, footer }) => (
  <div style={{ background: '#fff', border: '1px solid #e6e6ef', borderRadius: 16, boxShadow: '0 6px 16px rgba(12, 10, 36, 0.04)', padding: 16 }}>
    {title && <div style={{ fontWeight: 700, fontSize: 18, marginBottom: 8 }}>{title}</div>}
    <div>{children}</div>
    {footer}
  </div>
);
const Input = (props) => <input {...props} style={{ width: '100%', padding: '10px 12px', border: '1px solid #d6d6e6', borderRadius: 10, outline: 'none' }} />;
const Button = ({ children, ...p }) => <button {...p} style={{ padding: '10px 14px', borderRadius: 12, border: '1px solid #2f2bff22', background: '#4f46e5', color: '#fff', cursor: 'pointer' }}>{children}</button>;

function Row({ left, right }) {
  return <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, alignItems: 'center' }}><div>{left}</div><div>{right}</div></div>
}

// ---------------- Pages ----------------
function Login({ onAuthed }) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [err, setErr] = useState('');
  const [loading, setLoading] = useState(false);

  async function submit(e) {
    e.preventDefault();
    setErr('');
    setLoading(true);
    try {
      const r = await apiLogin({ email, password });
      // r: { accessToken, refreshToken, user: { id, name, email, role } }
      localStorage.setItem('accessToken', r.accessToken);
      localStorage.setItem('refreshToken', r.refreshToken);
      localStorage.setItem('user', JSON.stringify(r.user));
      onAuthed({ serverToken: r.accessToken, user: r.user });
    } catch (ex) {
      setErr(ex.message);
    } finally {
      setLoading(false);
    }
  }
  return <Card title="Login">
    {err && <p style={{ color: '#b00020' }}>{err}</p>}
    <form onSubmit={submit} style={{ display: 'grid', gap: 8 }}>
      <Input placeholder="Email" value={email} onChange={e => setEmail(e.target.value)} />
      <Input placeholder="Password" type="password" value={password} onChange={e => setPassword(e.target.value)} />
      <Button type="submit" disabled={loading}>{loading ? 'Signing in…' : 'Sign in'}</Button>
    </form>
  </Card>
}

function Signup({ onAuthed }) {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [err, setErr] = useState('');
  const [loading, setLoading] = useState(false);

  async function submit(e) {
    e.preventDefault();
    setErr('');
    setLoading(true);
    try {
      const r = await apiSignup({ name, email, password });
      localStorage.setItem('accessToken', r.accessToken);
      localStorage.setItem('refreshToken', r.refreshToken);
      localStorage.setItem('user', JSON.stringify(r.user));
      onAuthed({ serverToken: r.accessToken, user: r.user });
    } catch (ex) {
      setErr(ex.message);
    } finally {
      setLoading(false);
    }
  }
  return <Card title="Sign up">
    {err && <p style={{ color: '#b00020' }}>{err}</p>}
    <form onSubmit={submit} style={{ display: 'grid', gap: 8 }}>
      <Input placeholder="Full name" value={name} onChange={e => setName(e.target.value)} />
      <Input placeholder="Email" value={email} onChange={e => setEmail(e.target.value)} />
      <Input placeholder="Password" type="password" value={password} onChange={e => setPassword(e.target.value)} />
      <Button type="submit" disabled={loading}>{loading ? 'Creating…' : 'Create account'}</Button>
    </form>
  </Card>
}

function Doctors({ onPick, canBook }) {
  const [q, setQ] = useState('');
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState('');
  const [docs, setDocs] = useState([]);

  useEffect(() => {
    let mounted = true;
    (async () => {
      setLoading(true); setErr('');
      try {
        const list = await apiListDoctors();
        if (mounted) setDocs(list);
      } catch (e) {
        if (mounted) setErr(e.message);
      } finally {
        if (mounted) setLoading(false);
      }
    })();
    return () => { mounted = false; };
  }, []);

  const filtered = useMemo(() => docs.filter(d => !q ||
    (d.name || d.Name || '').toLowerCase().includes(q.toLowerCase()) ||
    (d.specialty || d.Specialty || '').toLowerCase().includes(q.toLowerCase())
  ), [docs, q]);

  return <Card title="Doctors">
    <div style={{ display: 'flex', gap: 8, marginBottom: 8 }}>
      <Input placeholder="Search name or specialty" value={q} onChange={e => setQ(e.target.value)} />
    </div>
    {loading && <p>Loading doctors…</p>}
    {err && <p style={{ color: '#b00020' }}>{err}</p>}
    {!loading && !err && (
      <div style={{ display: 'grid', gap: 8 }}>
        {filtered.map((d, i) => (
          <Row key={d.id || d.Id || i}
            left={<div>
              <div style={{ fontWeight: 600 }}>{d.name || d.Id || d.Name}</div>
              <div style={{ fontSize: 12, color: '#666' }}>{d.specialty || d.Specialty}</div>
            </div>}
            right={<Button onClick={() => onPick({
              id: d.id || d.Id,
              name: d.name || d.Name,
              specialty: d.specialty || d.Specialty,
              slots: d.slots || d.Slots || [],
            })}>View slots</Button>}
          />
        ))}
      </div>
    )}
    {!canBook && <p style={{ marginTop: 8, color: '#a86' }}>Log in to book.</p>}
  </Card>
}

function Availability({ doctor, me, token, onBooked }) {
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');
  const [bookedLocal, setBookedLocal] = useState(new Set());

  if (!doctor) return <Card title="Availability"><p>Select a doctor.</p></Card>;

  const allSlots = (doctor.slots || []).filter(Boolean);
  const filtered = allSlots
    .filter(s => (s || '').slice(0,10) === date)
    .filter(s => !bookedLocal.has(s)); // hide slots we just booked locally

  async function bookSlot(time) {
    setErr(''); setMsg('');
    try {
      const r = await apiCreateAppointment({
        token,
        patientName: me?.name || me?.email || 'Patient',
        doctorId: doctor.id,
        time,
      });
      setMsg(`Booked • ${r?.id || ''} with ${r?.doctorName || doctor.name} at ${new Date(time).toLocaleString()}`);
      setBookedLocal(new Set([...bookedLocal, time]));
      onBooked?.();
    } catch (e) {
      setErr(e.message);
    }
  }

  return <Card title={`Availability — ${doctor.name}`}>
    {msg && <p style={{ color: '#0a6' }}>{msg}</p>}
    {err && <p style={{ color: '#b00020' }}>{err}</p>}
    <div style={{ display: 'flex', gap: 8, marginBottom: 8 }}>
      <Input type="date" value={date} onChange={e => setDate(e.target.value)} />
    </div>
    {filtered.length === 0 ? <p>No free slots for selected date.</p> : (
      <div style={{ display: 'grid', gap: 6 }}>
        {filtered.map(s => (
          <Row key={s}
            left={<span>{new Date(s).toLocaleString()}</span>}
            right={<Button onClick={() => bookSlot(s)}>Book</Button>}
          />
        ))}
      </div>
    )}
  </Card>
}

function MyAppointments({ me, refresh }) {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState('');

  useEffect(() => {
    let mounted = true;
    (async () => {
      setLoading(true); setErr('');
      try {
        const list = await apiListAppointments();
        // Filter for current user if we have a name
        const myName = me?.name || me?.email || '';
        const mine = myName ? list.filter(a => (a.patientName || a.PatientName) === myName) : list;
        if (mounted) setItems(mine);
      } catch (e) {
        if (mounted) setErr(e.message);
      } finally {
        if (mounted) setLoading(false);
      }
    })();
    return () => { mounted = false; };
  }, [refresh, me?.name, me?.email]);

  return <Card title="My Appointments">
    {loading && <p>Loading…</p>}
    {err && <p style={{ color: '#b00020' }}>{err}</p>}
    {!loading && !err && (!items || items.length === 0) ? <p>No appointments yet.</p> : null}
    {!loading && !err && items?.length > 0 && (
      <div style={{ display: 'grid', gap: 6 }}>
        {items.map((a, idx) => (
          <Row key={a.id || a.Id || idx}
            left={<span>
              #{a.id || a.Id} — {a.doctorName || a.DoctorName || a.doctorId || a.DoctorId} — {new Date(a.time || a.Time).toLocaleString()}
            </span>}
            right={null /* backend has no cancel endpoint in the snippet */}
          />
        ))}
      </div>
    )}
  </Card>
}

// ---------------- Root App ----------------
export default function HealthApp() {
  const [tab, setTab] = useState('login');
  const [serverToken, setServerToken] = useState(null); // JWT from your backend
  const [me, setMe] = useState(null);                   // { id, name, email, role }
  const [doctor, setDoctor] = useState(null);
  const [refresh, setRefresh] = useState(0);

  // Restore session on refresh
  useEffect(() => {
    const t = localStorage.getItem('accessToken');
    const u = localStorage.getItem('user');
    if (t && u) {
      setServerToken(t);
      try { setMe(JSON.parse(u)); setTab('dashboard'); } catch { /* ignore */ }
    }
  }, []);

  function onAuthed({ serverToken, user }) {
    setServerToken(serverToken);
    setMe(user);
    setTab('dashboard');
  }

  function logout() {
    setServerToken(null);
    setMe(null);
    setDoctor(null);
    setTab('login');
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
  }

  return (
    <Shell>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <div style={{ width: 36, height: 36, borderRadius: 12, background: '#4f46e5' }} />
          <div>
            <div style={{ fontWeight: 800 }}>Health Booking</div>
            <div style={{ fontSize: 12, color: '#666' }}>{API_BASE}</div>
          </div>
        </div>
        <div>
          {me ? (
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <span style={{ fontSize: 14 }}>Hi, {me?.name || me?.email}</span>
              <button onClick={logout} style={{ padding: '8px 10px', borderRadius: 10, border: '1px solid #ddd', background: '#fff' }}>Logout</button>
            </div>
          ) : (
            <div style={{ display: 'flex', gap: 8 }}>
              <button onClick={() => setTab('login')} style={{ padding: '8px 10px', borderRadius: 10, border: '1px solid #ddd', background: tab === 'login' ? '#fff' : '#f2f2f8' }}>Login</button>
              <button onClick={() => setTab('signup')} style={{ padding: '8px 10px', borderRadius: 10, border: '1px solid #ddd', background: tab === 'signup' ? '#fff' : '#f2f2f8' }}>Signup</button>
            </div>
          )}
        </div>
      </div>

      {/* Pages */}
      {!me && tab === 'login' && <Login onAuthed={onAuthed} />}
      {!me && tab === 'signup' && <Signup onAuthed={onAuthed} />}

      {tab === 'dashboard' && (
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
          <Doctors onPick={setDoctor} canBook={!!me} />
          <Availability doctor={doctor} me={me} token={serverToken} onBooked={() => setRefresh(r => r + 1)} />
          <div style={{ gridColumn: '1 / -1' }}>
            <MyAppointments me={me} refresh={refresh} />
          </div>
        </div>
      )}

      <div style={{ marginTop: 16, fontSize: 12, color: '#666' }}>Tip: Sign up → Dashboard → pick a doctor → book (server) → see it in My Appointments.</div>
    </Shell>
  );
}
