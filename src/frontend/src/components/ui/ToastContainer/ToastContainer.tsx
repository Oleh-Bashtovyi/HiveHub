import { useEffect } from 'react';
import './ToastContainer.scss';

interface ToastContainerProps {
    message: string | null;
    onClose: () => void;
    duration?: number;
}

export const ToastContainer = ({ message, onClose, duration = 3000 }: ToastContainerProps) => {
    useEffect(() => {
        if (message) {
            const timer = setTimeout(() => {
                onClose();
            }, duration);

            return () => clearTimeout(timer);
        }
    }, [message, duration, onClose]);

    if (!message) return null;

    return (
        <div className="toast-container">
            <div className="toast-container__message">
                {message}
            </div>
        </div>
    );
};