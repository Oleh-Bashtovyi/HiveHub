import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Home } from './pages/Home/Home';
import {SpyGameProvider} from './context/SpyGameContext';
import {SpyEntry} from "./pages/SpyGame/SpyEntry/SpyEntry.tsx";
import {SpyLobby} from "./pages/SpyGame/SpyLobby/SpyLobby.tsx";
import {SpyGame} from "./pages/SpyGame/SpyGame/SpyGame.tsx";
import GlobalLoader from "./layout/GlobalLoader/GlobalLoader.tsx";

const SpyGameLayout = () => {
    return (
        <SpyGameProvider>
            <GlobalLoader />
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
                    <Route index element={<SpyEntry />} />
                    <Route path="lobby" element={<SpyLobby />} />
                    <Route path="game" element={<SpyGame />} />
                    {/*<Route path="results" element={<SpyResults />} />*/}
                </Route>

                <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
        </BrowserRouter>
    );
}

export default App;