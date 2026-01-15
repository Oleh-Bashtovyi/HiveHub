import { useState, useEffect } from 'react';

export const useGameTimer = (targetDateIso: string | null | undefined, isPaused: boolean = false) => {
    const [secondsLeft, setSecondsLeft] = useState(0);

    useEffect(() => {
        if (!targetDateIso || isPaused) return;

        const calculate = () => {
            const end = new Date(targetDateIso).getTime();
            const now = new Date().getTime();
            const diff = Math.floor((end - now) / 1000);
            setSecondsLeft(Math.max(0, diff));
        };

        calculate();
        const interval = setInterval(calculate, 1000);

        return () => clearInterval(interval);
    }, [targetDateIso, isPaused]);

    return secondsLeft;
};
