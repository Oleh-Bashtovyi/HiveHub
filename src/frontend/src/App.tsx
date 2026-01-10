import { BrowserRouter, Routes, Route, Navigate, Outlet } from 'react-router-dom';
import { Home } from './pages/Home/Home';
import { SpyGameProvider } from './context/SpyGameContext';
import {SpyEntry} from "./pages/SpyGame/SpyEntry/SpyEntry.tsx";

const SpyGameLayout = () => {
    return (
        <SpyGameProvider>
            <Outlet />
        </SpyGameProvider>
    );
};

function App() {
    return (
        <BrowserRouter>
            <Routes>
                {/* Public Home */}
                <Route path="/" element={<Home />} />

                {/* Spy Game Module */}
                <Route path="/spy" element={<SpyGameLayout />}>
                    {/*<Route path="game" element={<SpyGame />} />*/}
                    <Route index element={<SpyEntry />} />
{/*                    <Route index element={<SpyEntry />} />
                    <Route path="lobby" element={<SpyLobby />} />
                    <Route path="results" element={<SpyResults />} />*/}
                </Route>

                <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
        </BrowserRouter>
    );
}

export default App;