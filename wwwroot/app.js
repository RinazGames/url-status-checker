// Глобальные переменные.
let chartInstance = null;
const COLORS = {
    statusUp: 'status-up',
    statusDown: 'status-down',
    statusUnknown: 'status-unknown',

    chartLineUp: 'rgba(75, 192, 192, 1)',
    chartFillUp: 'rgba(75, 192, 192, 0.6)',

    chartLineDown: 'rgba(255, 99, 132, 1)',
    chartFillDown: 'rgba(255, 99, 132, 0.6)'
};

// Функция для экранирования html-строк.
function escapeHtml(unsafe) {
    if (!unsafe) return '';
    return unsafe
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}


// Слушатель события полной загрузки DOM-дерева.
document.addEventListener('DOMContentLoaded', () => {
    // Сначала загружаем список сайтов.
    loadSites();

    // Регистрация обработчика отправки формы добавления сайта.
    document.getElementById('addSiteForm').addEventListener('submit', async (e) => {
        // Предотвращение стандартной перезагрузки страницы при отправке формы.
        e.preventDefault();

        // Формируем объект с данными нового сайта.
        const newSite = {
            name: document.getElementById('siteName').value,
            url: document.getElementById('siteUrl').value,
            category: document.getElementById('siteCategory').value,
            checkInterval: parseInt(document.getElementById('siteInterval').value),
            isActive: true
        };

        try {
            // Отправка POST-запроса на сервер с данными нового сайта.
            const response = await fetch('/api/sites', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(newSite)
            });

            // Если сервер ответил успешно (статус 2xx).
            if (response.ok)
            {
                // Закрываем модальное окно.
                bootstrap.Modal.getInstance(document.getElementById('addSiteModal')).hide();
                // Сбрасываем значения формы.
                document.getElementById('addSiteForm').reset();
                // Обновляем список сайтов.
                loadSites();
            }
            // Если произошла ошибка.
            else {
                alert('Ошибка при добавлении сайта');
            }
        } catch (error) {
            // Логирование ошибки сети.
            console.error(error);
            alert('Ошибка сети');
        }
    });
});


// Асинхронная функция загрузки и отображения списка сайтов.
async function loadSites() {
    try {
        // Получение общего списка сайтов через GET-запрос к API.
        const response = await fetch('/api/sites');
        const sites = await response.json();

        // Получаем элемент страницы с списком сайтов и очищаем.
        const tbody = document.getElementById('sitesTableBody');
        tbody.innerHTML = '';

        // Если сайтов нет.
        if (sites.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center">Нет добавленных сайтов. Добавьте первый!</td></tr>';
            return;
        }

        // Перебираем каждый полученный сайт.
        for (const site of sites) {
            // Запрос истории проверок конкретного сайта для определения статуса.
            const historyRes = await fetch(`/api/sites/${site.id}/history`);
            const history = await historyRes.json();

            // Сортировка записей по времени по возрастанию.
            history.sort((a, b) => new Date(a.timestamp) - new Date(b.timestamp));

            // Извлечение последней записи.
            const lastCheck = history.length > 0 ? history[history.length - 1] : null;

            // Ставим css цвет в зависимости от доступности сайта.
            const statusClass = lastCheck
                ? (lastCheck.isAvailable ? COLORS.statusUp : COLORS.statusDown)
                : COLORS.statusUnknown;
            // Ставим текст в зависимости от статуса сайта.
            const statusText = lastCheck
                ? (lastCheck.isAvailable ? `UP (${lastCheck.statusCode})` : `DOWN (${lastCheck.errorMessage || lastCheck.statusCode})`)
                : 'Не проверялся';
            // Форматируем дату и время последней проверки.
            const lastCheckTime = lastCheck
                ? new Date(lastCheck.timestamp).toLocaleString('ru-RU')
                : '—';

            // Экранируем данные для защиты.
            const safeName = escapeHtml(site.name);
            const safeUrl = escapeHtml(site.url);
            const safeCategory = escapeHtml(site.category || 'Без категории');
            const safeNameForJs = safeName.replace(/'/g, "\\'");

            // Ставим HTML-разметку с защищенными данными сайта.
            const row = `
                <tr>
                    <td><strong>${safeName}</strong></td>
                    <td><a href="${safeUrl}" target="_blank">${safeUrl}</a></td>
                    <td><span class="badge bg-secondary">${safeCategory}</span></td>
                    <td class="${statusClass}">${statusText}</td>
                    <td>${lastCheckTime}</td>
                    <td>
                        <button class="btn btn-sm btn-outline-primary me-1" onclick="checkSite('${site.id}')" title="Проверить сейчас">
                            <i class="bi bi-arrow-clockwise"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-info" onclick="showHistory('${site.id}', '${safeNameForJs}')" title="История и график">
                            <i class="bi bi-graph-up"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="deleteSite('${site.id}')" title="Удалить">
                            <i class="bi bi-trash"></i>
                        </button>
                    </td>
                </tr>
            `;
            tbody.innerHTML += row;
        }
    } catch (error) {
        console.error("Ошибка загрузки сайтов:", error);
    }
}

// Асинхронная функция принудительной проверки доступности сайта.
async function checkSite(id) {
    // Ищем кнопку, инициировавшей событие клика.
    const btn = event.target.closest('button');
    // Сохраняем исходный вид кнопки.
    const originalIcon = btn.innerHTML;
    // Блокируем кнопку.
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm"></span>';    
    
    try
    {
        // Отправка POST-запроса для запуска процедуры проверки на сервере.
        await fetch(`/api/sites/${id}/check`, { method: 'POST' });
        // Ждём завершения фоновой операции на сервере.
        await new Promise(r => setTimeout(r, 500));
        // Обновляем список сайтов.
        await loadSites();
    }
    catch (error) {
        console.error(error);
    }
    finally
    {
        // Восстанавливаем кнопку.
        btn.disabled = false;
        btn.innerHTML = originalIcon;
    }
}

// Асинхронная функция удаления сайта из системы.
async function deleteSite(id) {
    // Подтверждение действия.
    if (confirm('Вы уверены, что хотите удалить этот сайт?'))
    {
        try {
            // Отправка DELETE-запроса на удаление ресурса по указанному ID.
            await fetch(`/api/sites/${id}`, { method: 'DELETE' });
            // Обновляем список сайтов.
            loadSites();
        } catch (error) {
            console.error(error);
        }
    }
}

// Асинхронная функция отображения графика истории проверок сайта.
async function showHistory(id, name) {
    // Инициализируем модальное окно с графиком истории.
    document.getElementById('historyModalTitle').innerText = `История: ${name}`;
    const modal = new bootstrap.Modal(document.getElementById('historyModal'));
    modal.show();

    try
    {
        // Запрос полной истории проверок выбранного сайта.
        const response = await fetch(`/api/sites/${id}/history`);
        let history = await response.json();

        // Сортировка истории по времени и выбор последних двадцати записей.
        history.sort((a, b) => new Date(a.timestamp) - new Date(b.timestamp));
        const recentHistory = history.slice(-20);

        // Формируем массив подписей осей для графика.
        const labels = recentHistory.map(h => new Date(h.timestamp).toLocaleString('ru-RU', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        }));
        const dataPoints = recentHistory.map(h => h.responseTimeMs);

        // Назначаем цвета точек графика в зависимости от статуса доступности.
        const backgroundColors = recentHistory.map(h => h.isAvailable ? COLORS.chartFillUp : COLORS.chartFillDown);

        // Получаем canvas элемента графика.
        const ctx = document.getElementById('historyChart').getContext('2d');

        // Уничтожение предыдущего экземпляра графика.
        if (chartInstance) {
            chartInstance.destroy();
        }

        // Создание нового линейного графика с данными.
        chartInstance = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Время отклика (мс)',
                    data: dataPoints,
                    borderColor: COLORS.chartLineUp,
                    backgroundColor: backgroundColors,
                    borderWidth: 2,
                    tension: 0.3,
                    pointRadius: 4
                }]
            },
            options: {
                responsive: true,
                scales: {
                    y: { beginAtZero: true, title: { display: true, text: 'Миллисекунды' } }
                }
            }
        });
    }
    catch (error) {
        console.error("Ошибка загрузки истории:", error);
    }
}