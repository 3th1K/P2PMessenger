# P2P Messenger Application

This is a peer-to-peer (P2P) messenger application built using .NET WPF (Windows Presentation Foundation). It allows two end users to establish a secure connection and communicate with each other via text chat. The communication is encrypted using multiple layers of encryption and decrypted using a shared key.

## Features

- **P2P Communication**: The application facilitates direct communication between two users without the need for a centralized server.
- **Secure Connection**: All communication between users is encrypted to ensure privacy and security.
- **Shared Key Encryption**: Messages are encrypted using a shared key, ensuring that only the intended recipient can decrypt and read them.
- **Multi-layer Encryption**: Messages undergo multiple layers of encryption to further enhance security.
- **User-friendly Interface**: The application is designed with a user-friendly interface using WPF for a seamless user experience.

## Getting Started

### Prerequisites

- .NET Framework
- Visual Studio (or any other compatible IDE for .NET development)

### Installation

1. Clone the repository to your local machine:

    ```bash
    git clone https://github.com/3th1K/P2PMessenger.git
    ```

2. Open the solution file `P2PMessenger.sln` in Visual Studio.

3. Build the solution to resolve any dependencies and compile the application.

### Usage

1. Build the project to generate the executable file. You can do this by opening the solution file `P2PMessenger.sln` in Visual Studio and building the solution.

2. Once the project is built, navigate to the directory containing the executable file (`P2PMessenger.exe`).

3. Run the executable file from the command line with the username argument. For example:

    ```bash
    P2PMessenger.exe Alice
    ```

   Replace `Alice` with the username (Alice or Bob) you want to use for the current instance of the application.

4. Repeat the above steps on another machine or another instance of the application, specifying the other username (e.g., `Bob`), to establish a connection between the two users.

5. Enter the shared key to establish a secure connection.

6. Start communicating by typing messages in the chat interface.

7. Messages will be encrypted before transmission and decrypted upon receipt using the shared key.

### Contributing

Contributions are welcome! If you'd like to contribute to this project, please follow these steps:

1. Fork the repository.
2. Create a new branch (`git checkout -b feature/improvement`)
3. Make your changes and commit them (`git commit -am 'Add new feature'`)
4. Push to the branch (`git push origin feature/improvement`)
5. Create a new Pull Request.


### Acknowledgements

- Special thanks to the developers of libraries and frameworks used in this project.
- Inspired by the need for secure and private communication channels.
