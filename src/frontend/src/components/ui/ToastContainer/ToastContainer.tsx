import { useEffect } from 'react';
import './ToastContainer.scss';

export type ToastType = 'info' | 'error' | 'success';

interface ToastContainerProps {
    message: string | null;
    type?: ToastType; // Додаємо тип
    onClose: () => void;
    duration?: number;
}

export const ToastContainer = ({
                                   message,
                                   type = 'info',
                                   onClose,
                                   duration = 3000
                               }: ToastContainerProps) => {

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
            <div className={`toast-container__message toast-container__message--${type}`}>
                <span className="toast-container__icon">
                    {type === 'error' && '⚠️'}
                    {type === 'success' && '✅'}
                    {type === 'info' && 'ℹ️'}
                </span>
                {message}
            </div>
        </div>
    );
};