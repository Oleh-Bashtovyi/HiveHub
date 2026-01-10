import React from 'react';
import classNames from 'classnames';
import './Button.scss';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
    variant?: 'primary' | 'secondary' | 'danger';
    fullWidth?: boolean;
    size?: 'small' | 'medium';
    isLoading?: boolean;
}

export const Button: React.FC<ButtonProps> = ({
                                                  children,
                                                  className,
                                                  variant = 'primary',
                                                  fullWidth = false,
                                                  size = 'medium',
                                                  isLoading = false,
                                                  disabled,
                                                  ...props
                                              }) => {
    return (
        <button
            className={classNames('app-btn', `btn-${variant}`, {
                'btn-full': fullWidth,
                'btn-small': size === 'small'
            }, className)}
            disabled={disabled || isLoading}
            {...props}
        >
            {isLoading ? 'Wait...' : children}
        </button>
    );
};