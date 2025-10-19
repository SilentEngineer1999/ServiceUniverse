import React, { useEffect, useMemo, useState } from "react";


function createMockAPI({ days = 60, slotMinutes = 30, startHour = 9, everyNthDay = 2 } = {}) {
  let users = []; // {id,email,hash,role}
  let patients = []; // {id,userId,name}
  const doctors = [
    { id: "d1", name: "Dr. A. Rahman", specialty: "GP" },
    { id: "d2", name: "Dr. S. Islam", specialty: "Cardiology" },
    { id: "d3", name: "Dr. F. Chowdhury", specialty: "Dermatology" },
    { id: "d4", name: "Dr. M. Hasan", specialty: "Neurology" },
    { id: "d5", name: "Dr. T. Akter", specialty: "Pediatrics" },
    { id: "d6", name: "Dr. N. Karim", specialty: "Endocrinology" },
    { id: "d7", name: "Dr. R. Chowdhury", specialty: "Orthopedics" },
    { id: "d8", name: "Dr. L. Ahmed", specialty: "Ophthalmology" },
    { id: "d9", name: "Dr. P. Das", specialty: "ENT" },
    { id: "d10", name: "Dr. Z. Hossain", specialty: "Psychiatry" }
  ];
  let slots = []; // {id, doctorId, start, end, isBooked}
  let appts = []; // {id, patientId, doctorId, slotId, status}
  let uid = 1, sid = 1, aid = 1;

  function seedSlots() {
    // Generate ~2 months of slots per doctor, every 2nd day, alternating 6/7 slots from 09:00.
    const now = new Date();
    for (const doc of doctors) {
      for (let d = 0; d < days; d += everyNthDay) {
        const base = new Date(
          now.getFullYear(),
          now.getMonth(),
          now.getDate() + d,
          startHour, 0, 0, 0
        );
        const perDay = ((d / everyNthDay) % 2 === 0) ? 6 : 7; // alternate 6 / 7
        for (let i = 0; i < perDay; i++) {
          const start = new Date(base.getTime() + i * slotMinutes * 60000);
          const end = new Date(start.getTime() + slotMinutes * 60000);
          slots.push({
            id: sid++, doctorId: doc.id,
            start: start.toISOString(), end: end.toISOString(),
            isBooked: false
          });
        }
      }
    }
  }
  seedSlots();

  const h = (s) => Array.from(s).reduce((a, c) => ((a * 31 + c.charCodeAt(0)) | 0), 0).toString(16);

  function signup({ name, email, password }) {
    email = email.trim().toLowerCase();
    if (users.some(u => u.email === email)) throw new Error("Email already registered");
    const u = { id: uid++, email, hash: h(password), role: "patient" }; users.push(u);
    const p = { id: patients.length + 1, userId: u.id, name }; patients.push(p);
    return { token: String(u.id), userId: u.id, role: u.role, patient: p };
  }
  function login({ email, password }) {
    email = email.trim().toLowerCase();
    const u = users.find(u => u.email === email);
    if (!u || u.hash !== h(password)) throw new Error("Invalid credentials");
    const p = patients.find(p => p.userId === u.id) || null;
    return { token: String(u.id), userId: u.id, role: u.role, patient: p };
  }
  function me(token) {
    const u = users.find(x => String(x.id) === String(token));
    if (!u) throw new Error("Unauthorized");
    const p = patients.find(p => p.userId === u.id) || null;
    return { id: u.id, email: u.email, role: u.role, patient: p };
  }
  function listDoctors() { return [...doctors]; }
  function listAvailability(doctorId) {
    const cutoff = Date.now() - 60000; // hide slots that already ended
    return slots
      .filter(s => s.doctorId === doctorId && !s.isBooked && new Date(s.end).getTime() > cutoff)
      .sort((a, b) => new Date(a.start) - new Date(b.start));
  }
  function book(token, { doctorId, slotId }) {
    const u = users.find(x => String(x.id) === String(token));
    if (!u) throw new Error("Unauthorized");
    const p = patients.find(p => p.userId === u.id);
    if (!p) throw new Error("Not a patient");
    const s = slots.find(x => x.id === slotId && x.doctorId === doctorId);
    if (!s) throw new Error("Slot not found");
    if (s.isBooked) throw new Error("Slot already booked");
    s.isBooked = true;
    const a = { id: aid++, patientId: p.id, doctorId, slotId, status: "booked" };
    appts.push(a);
    return a;
  }
  function myAppointments(token) {
    const u = users.find(x => String(x.id) === String(token));
    if (!u) throw new Error("Unauthorized");
    const p = patients.find(p => p.userId === u.id);
    if (!p) throw new Error("Not a patient");
    return appts.filter(a => a.patientId === p.id).sort((a, b) => b.id - a.id);
  }
  function cancel(token, id) {
    const u = users.find(x => String(x.id) === String(token));
    if (!u) throw new Error("Unauthorized");
    const p = patients.find(p => p.userId === u.id);
    if (!p) throw new Error("Not a patient");
    const a = appts.find(x => x.id === id && x.patientId === p.id);
    if (!a) throw new Error("Appointment not found");
    if (a.status === "cancelled") throw new Error("Already cancelled");
    a.status = "cancelled";
    const s = slots.find(s => s.id === a.slotId);
    if (s) s.isBooked = false;
    return a;
  }

  return { signup, login, me, listDoctors, listAvailability, book, myAppointments, cancel, __debug: () => ({ users, patients, doctors, slots, appts }) };
}

// Live preview API instance
const MockAPI = createMockAPI({});

// ---------------- UI Primitives ----------------
const Shell = ({ children }) => (
  <div style={{ minHeight: '100vh', background: '#f7f7fb', color: '#111' }}>
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
  function submit(e) { e.preventDefault(); setErr(''); try { const r = MockAPI.login({ email, password }); onAuthed(r.token); } catch (ex) { setErr(ex.message); } }
  return <Card title="Login">
    {err && <p style={{ color: '#b00020' }}>{err}</p>}
    <form onSubmit={submit} style={{ display: 'grid', gap: 8 }}>
      <Input placeholder="Email" value={email} onChange={e => setEmail(e.target.value)} />
      <Input placeholder="Password" type="password" value={password} onChange={e => setPassword(e.target.value)} />
      <Button type="submit">Sign in</Button>
    </form>
  </Card>
}

function Signup({ onAuthed }) {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [err, setErr] = useState('');
  function submit(e) { e.preventDefault(); setErr(''); try { const r = MockAPI.signup({ name, email, password }); onAuthed(r.token); } catch (ex) { setErr(ex.message); } }
  return <Card title="Sign up">
    {err && <p style={{ color: '#b00020' }}>{err}</p>}
    <form onSubmit={submit} style={{ display: 'grid', gap: 8 }}>
      <Input placeholder="Full name" value={name} onChange={e => setName(e.target.value)} />
      <Input placeholder="Email" value={email} onChange={e => setEmail(e.target.value)} />
      <Input placeholder="Password" type="password" value={password} onChange={e => setPassword(e.target.value)} />
      <Button type="submit">Create account</Button>
    </form>
  </Card>
}

function Doctors({ onPick, canBook }) {
  const [q, setQ] = useState('');
  const docs = useMemo(() => MockAPI.listDoctors(), []);
  const filtered = useMemo(() => docs.filter(d => !q || d.name.toLowerCase().includes(q.toLowerCase()) || d.specialty.toLowerCase().includes(q.toLowerCase())), [docs, q]);
  return <Card title="Doctors">
    <div style={{ display: 'flex', gap: 8, marginBottom: 8 }}>
      <Input placeholder="Search name or specialty" value={q} onChange={e => setQ(e.target.value)} />
    </div>
    <div style={{ display: 'grid', gap: 8 }}>
      {filtered.map(d => (
        <Row key={d.id}
          left={<div><div style={{ fontWeight: 600 }}>{d.name}</div><div style={{ fontSize: 12, color: '#666' }}>{d.specialty}</div></div>}
          right={<Button onClick={() => onPick(d)}>View slots</Button>}
        />
      ))}
    </div>
    {!canBook && <p style={{ marginTop: 8, color: '#a86' }}>Log in to book.</p>}
  </Card>
}

function Availability({ doctor, token, onBooked }) {
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [slots, setSlots] = useState([]);
  const [msg, setMsg] = useState('');
  useEffect(() => { if (doctor) setSlots(MockAPI.listAvailability(doctor.id)); }, [doctor]);
  const filtered = useMemo(() => slots.filter(s => s.start.slice(0, 10) === date), [slots, date]);
  function bookSlot(s) { try { const r = MockAPI.book(token, { doctorId: doctor.id, slotId: s.id }); setMsg(`Booked #${r.id}`); setSlots(MockAPI.listAvailability(doctor.id)); onBooked?.(); } catch (ex) { setMsg(ex.message); } }
  if (!doctor) return <Card title="Availability"><p>Select a doctor.</p></Card>;
  return <Card title={`Availability — ${doctor.name}`}>
    {msg && <p style={{ color: '#0a6' }}>{msg}</p>}
    <div style={{ display: 'flex', gap: 8, marginBottom: 8 }}>
      <Input type="date" value={date} onChange={e => setDate(e.target.value)} />
    </div>
    {filtered.length === 0 ? <p>No free slots for selected date.</p> : (
      <div style={{ display: 'grid', gap: 6 }}>
        {filtered.map(s => (
          <Row key={s.id}
            left={<span>{new Date(s.start).toLocaleString()} → {new Date(s.end).toLocaleString()}</span>}
            right={<Button onClick={() => bookSlot(s)}>Book</Button>}
          />
        ))}
      </div>
    )}
  </Card>
}

function MyAppointments({ token, refresh }) {
  const [items, setItems] = useState([]);
  const [msg, setMsg] = useState('');
  function reload() { try { setItems(MockAPI.myAppointments(token)); } catch { setItems([]); } }
  useEffect(() => { if (token) reload(); }, [token, refresh]);
  function cancel(id) { try { MockAPI.cancel(token, id); setMsg('Cancelled.'); reload(); } catch (ex) { setMsg(ex.message); } }
  return <Card title="My Appointments">
    {msg && <p style={{ color: '#0a6' }}>{msg}</p>}
    {(!items || items.length === 0) ? <p>No appointments yet.</p> : (
      <div style={{ display: 'grid', gap: 6 }}>
        {items.map(a => (
          <Row key={a.id}
            left={<span>#{a.id} — Doctor: {a.doctorId} — Slot: {a.slotId} — <b style={{ color: a.status === 'booked' ? '#0a6' : '#666' }}>{a.status}</b></span>}
            right={a.status === 'booked' && <button onClick={() => cancel(a.id)} style={{ padding: '8px 12px', borderRadius: 10, border: '1px solid #ddd', background: '#fff' }}>Cancel</button>}
          />
        ))}
      </div>
    )}
  </Card>
}

// ---------------- Self-Tests (do not modify existing tests; add new as needed) ----------------
function TestsPanel() {
  const [running, setRunning] = useState(false);
  const [results, setResults] = useState([]);

  async function run() {
    setRunning(true);
    const api = createMockAPI({}); // isolated instance for tests
    const out = [];

    function pass(name) { out.push({ name, ok: true }); }
    function fail(name, err) { out.push({ name, ok: false, err: String(err) }); }

    let token, doc, slot, appt;

    // Test 1: signup -> creates patient
    try {
      const r = api.signup({ name: 'Tester', email: 'tester@example.com', password: 'pw' });
      token = r.token;
      const me = api.me(token);
      if (me.patient) pass('Signup creates Patient automatically');
      else throw new Error('No patient created');
    } catch (e) { fail('Signup creates Patient automatically', e); }

    // Test 2: doctors count
    try {
      const list = api.listDoctors();
      if (list.length === 10) pass('Lists exactly 10 doctors');
      else throw new Error(`Expected 10, got ${list.length}`);
    } catch (e) { fail('Lists exactly 10 doctors', e); }

    // Test 3: availability exists for a doctor
    try {
      const list = api.listDoctors();
      doc = list[0];
      const av = api.listAvailability(doc.id);
      if (av.length > 0) { slot = av[0]; pass('Availability returns future slots'); }
      else throw new Error('No future slots');
    } catch (e) { fail('Availability returns future slots', e); }

    // Test 4: book consumes the slot
    try {
      const before = api.listAvailability(doc.id).length;
      appt = api.book(token, { doctorId: doc.id, slotId: slot.id });
      const after = api.listAvailability(doc.id).length;
      if (after === before - 1) pass('Booking consumes one slot');
      else throw new Error(`Expected ${before - 1}, got ${after}`);
    } catch (e) { fail('Booking consumes one slot', e); }

    // Test 5: my appointments includes the booking
    try {
      const my = api.myAppointments(token);
      if (my.find(a => a.id === appt.id)) pass('My Appointments includes the new booking');
      else throw new Error('Booking not found in My Appointments');
    } catch (e) { fail('My Appointments includes the new booking', e); }

    // Test 6: cancel frees the slot
    try {
      api.cancel(token, appt.id);
      const av2 = api.listAvailability(doc.id);
      if (av2.find(s => s.id === slot.id)) pass('Cancelling frees the slot');
      else throw new Error('Cancelled slot not freed');
    } catch (e) { fail('Cancelling frees the slot', e); }

    setResults(out);
    setRunning(false);
  }

  return (
    <Card title="Self-Tests">
      <div style={{ display: 'flex', gap: 8, alignItems: 'center', marginBottom: 8 }}>
        <Button disabled={running} onClick={run}>{running ? 'Running…' : 'Run tests'}</Button>
        <span style={{ fontSize: 12, color: '#666' }}>Tests run against an isolated in-memory API instance.</span>
      </div>
      {results.length > 0 && (
        <ul style={{ display: 'grid', gap: 6 }}>
          {results.map((r, i) => (
            <li key={i} style={{ color: r.ok ? '#0a6' : '#b00020' }}>
              {r.ok ? '✔︎' : '✘'} {r.name}{!r.ok && ` — ${r.err}`}
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}

// ---------------- Root App ----------------
export default function App() {
  const [tab, setTab] = useState('login');
  const [token, setToken] = useState(null);
  const [me, setMe] = useState(null);
  const [doctor, setDoctor] = useState(null);
  const [refresh, setRefresh] = useState(0);

  function onAuthed(tok) { setToken(tok); try { setMe(MockAPI.me(tok)); } catch { setMe(null); } setTab('dashboard'); }
  function logout() { setToken(null); setMe(null); setDoctor(null); setTab('login'); }

  return (
    <Shell>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <div style={{ width: 36, height: 36, borderRadius: 12, background: '#4f46e5' }} />
          <div>
            <div style={{ fontWeight: 800 }}>Health Booking</div>
            <div style={{ fontSize: 12, color: '#666' }}></div>
          </div>
        </div>
        <div>
          {me ? (
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <span style={{ fontSize: 14 }}>Hi, {me.patient?.name || me.email}</span>
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
          <Availability doctor={doctor} token={token} onBooked={() => setRefresh(r => r + 1)} />
          <div style={{ gridColumn: '1 / -1' }}>
            <MyAppointments token={token} refresh={refresh} />
          </div>
        </div>
      )}

      <div style={{ marginTop: 16, fontSize: 12, color: '#666' }}>Tip: Sign up → Dashboard → pick a doctor → book → cancel.</div>

      {/* <div style={{ marginTop: 16 }}>
        <TestsPanel />
      </div> */}
    </Shell>
  );
}
