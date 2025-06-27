var notifications = [
    { title: "كرسي مكتب 50×50سم - MADE146", content: "5,969.00 EGP", image: "chair1.avif" },
    { title: "كرسي مكتب 50×50سم - MADE147", content: "6,200.00 EGP", image: "chair2.avif" },
    { title: "كرسي مكتب 50×50سم - MADE148", content: "3,800.00 EGP", image: "chair3.avif" },
    { title: "كرسي مكتب 5   0×50سم - MADE149", content: "2,000.00 EGP", image: "chair1.avif" },
    { title: "كرسي مكتب 50×50سم - MADE146", content: "5,969.00 EGP", image: "chair1.avif" },
    { title: "كرسي مكتب 50×50سم - MADE147", content: "6,200.00 EGP", image: "chair1.avif" },
    { title: "كرسي مكتب 50×50سم - MADE148", content: "3,800.00 EGP", image: "chair1.avif" },
    { title: "كرسي مكتب 50×50سم - MADE149", content: "2,000.00 EGP", image: "chair1.avif" },
    { title: "كرسي مكتب 50×50سم - MADE146", content: "5,969.00 EGP", image: "chair1.avif" },
    { title: "كرسي مكتب 50×50سم - MADE147", content: "6,200.00 EGP", image: "chair1.avif" },
    { title: "كرسي مكتب 50×50سم - MADE148", content: "3,800.00 EGP", image: "chair1.avif" },
    { title: "كرسي مكتب 5   0×50سم - MADE149", content: "2,000.00 EGP", image: "chair1.avif" },
    { title: "كرسي مكتب 50×50سم - MADE146", content: "5,969.00 EGP", image:"chair1.avif" },
    { title: "كرسي مكتب 50×50سم - MADE147", content: "6,200.00 EGP", image: "chair1.avif" }
];

notifications = notifications.map((notif, idx) => ({
  ...notif,
  image: `chair${idx + 1}.jpg`
}));
notifications = notifications.map((notif, idx) => ({
  ...notif,
  title: `كرسي مكتب`

}));

notifications = notifications.map((notif, idx) => ({
  ...notif,
  content: `00.00 EGP`
  
}));

var currentPage = 1;
var notificationsPerPage = 6; // Reduced for testing

function displayNotifications(page) {
    var startIndex = (page - 1) * notificationsPerPage;
    var endIndex = startIndex + notificationsPerPage;
    var notificationsToShow = notifications.slice(startIndex, endIndex);

    var notificationsContainer = document.querySelector('.grid'); // Correct selector
    notificationsContainer.innerHTML = '';

    notificationsToShow.forEach(function(notification) {
        var notificationElement = document.createElement('div');
        notificationElement.classList.add('product-card');
        notificationElement.innerHTML = `
            <img src="image/Chairs/${notification.image}" alt="Product Image">
            <h3>${notification.title}</h3>
            <p>${notification.content}</p>
            <button>اضف إلى العربة</button>
        `;
        notificationsContainer.appendChild(notificationElement);
    });
}

function displayPagination() {
    var totalNotifications = notifications.length;
    var totalPages = Math.ceil(totalNotifications / notificationsPerPage);
    var paginationContainer = document.getElementById('btn-group');
    paginationContainer.classList.add('center');

    for (var i = 1; i <= totalPages; i++) {
        var button = document.createElement('input');
        button.classList.add('btn-check');
        button.classList.add('btn-check');
        button.type = 'radio';
        button.id = 'btnradio' + i;
        button.name = 'btnradio';
        button.value = i; // Set the value of the button to the page number
        button.addEventListener('click', function() {
            currentPage = parseInt(this.value);
            displayNotifications(currentPage);
        });

        var label = document.createElement('label');
        label.setAttribute('for', 'btnradio' + i);
        label.textContent = i;
        label.classList.add('btn', 'btn-outline-primary');
        paginationContainer.appendChild(button);
        paginationContainer.appendChild(label);
    }
}



function updatePaginationButtons() {
    document.querySelectorAll('#btn-group button').forEach((btn, index) => {
        btn.classList.toggle('active', index + 1 === currentPage);
    });
}

// Initial Display
displayNotifications(currentPage);
displayPagination();
