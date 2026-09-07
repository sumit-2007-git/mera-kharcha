// Expense Split Calculation Engine & Mock Receipt Scanner
document.addEventListener('DOMContentLoaded', function () {
    const baseAmountInput = document.getElementById('BaseAmount');
    const taxInput = document.getElementById('TaxAmount');
    const tipInput = document.getElementById('TipAmount');
    const serviceInput = document.getElementById('ServiceCharge');
    const discountInput = document.getElementById('DiscountAmount');
    const finalTotalDisplay = document.getElementById('finalTotalDisplay');
    const splitTypeSelect = document.getElementById('splitTypeSelect');
    const splitRows = document.querySelectorAll('.participant-split-row');

    function calculateFinalTotal() {
        const base = parseFloat(baseAmountInput?.value) || 0;
        const tax = parseFloat(taxInput?.value) || 0;
        const tip = parseFloat(tipInput?.value) || 0;
        const service = parseFloat(serviceInput?.value) || 0;
        const discount = parseFloat(discountInput?.value) || 0;

        const total = Math.max(0, (base + tax + tip + service) - discount);
        if (finalTotalDisplay) {
            finalTotalDisplay.innerText = '₹' + total.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        }
        return total;
    }

    function updateSplitInputsVisibility() {
        const selectedType = splitTypeSelect ? splitTypeSelect.value : "1"; // 1: Equal, 2: Exact, 3: Percentage, 4: Shares

        document.querySelectorAll('.split-col-exact').forEach(el => el.classList.toggle('d-none', selectedType !== "2" && selectedType !== "ExactAmount"));
        document.querySelectorAll('.split-col-percentage').forEach(el => el.classList.toggle('d-none', selectedType !== "3" && selectedType !== "Percentage"));
        document.querySelectorAll('.split-col-shares').forEach(el => el.classList.toggle('d-none', selectedType !== "4" && selectedType !== "Shares"));

        recalculateShares();
    }

    function recalculateShares() {
        const totalAmount = calculateFinalTotal();
        const selectedType = splitTypeSelect ? splitTypeSelect.value : "1";

        let includedCheckboxes = document.querySelectorAll('.participant-checkbox:checked');
        let count = includedCheckboxes.length;

        if (count === 0) {
            updateStatusBadge("Please select at least one participant", false);
            return;
        }

        if (selectedType === "1" || selectedType === "Equal") {
            const equalShare = count > 0 ? (totalAmount / count).toFixed(2) : 0;
            splitRows.forEach(row => {
                const cb = row.querySelector('.participant-checkbox');
                const badge = row.querySelector('.calculated-share-badge');
                if (badge) {
                    if (cb && cb.checked) {
                        badge.innerText = '₹' + parseFloat(equalShare).toLocaleString('en-IN', { minimumFractionDigits: 2 });
                    } else {
                        badge.innerText = '₹0.00';
                    }
                }
            });
            updateStatusBadge(`Evenly split: ₹${equalShare} / person`, true);
        }
        else if (selectedType === "2" || selectedType === "ExactAmount") {
            let sumExact = 0;
            splitRows.forEach(row => {
                const cb = row.querySelector('.participant-checkbox');
                const exactInp = row.querySelector('.exact-amount-input');
                const badge = row.querySelector('.calculated-share-badge');
                if (cb && cb.checked && exactInp) {
                    const val = parseFloat(exactInp.value) || 0;
                    sumExact += val;
                    if (badge) badge.innerText = '₹' + val.toFixed(2);
                } else if (badge) {
                    badge.innerText = '₹0.00';
                }
            });

            const diff = (totalAmount - sumExact).toFixed(2);
            if (Math.abs(diff) < 0.05) {
                updateStatusBadge(`Exact amounts match total perfectly! (₹${sumExact.toFixed(2)})`, true);
            } else if (diff > 0) {
                updateStatusBadge(`Remaining to assign: ₹${diff}`, false);
            } else {
                updateStatusBadge(`Exceeds total by: ₹${Math.abs(diff)}`, false);
            }
        }
        else if (selectedType === "3" || selectedType === "Percentage") {
            let sumPct = 0;
            splitRows.forEach(row => {
                const cb = row.querySelector('.participant-checkbox');
                const pctInp = row.querySelector('.percentage-input');
                const badge = row.querySelector('.calculated-share-badge');
                if (cb && cb.checked && pctInp) {
                    const pct = parseFloat(pctInp.value) || 0;
                    sumPct += pct;
                    const share = (totalAmount * (pct / 100)).toFixed(2);
                    if (badge) badge.innerText = '₹' + share;
                } else if (badge) {
                    badge.innerText = '₹0.00';
                }
            });

            const diffPct = (100 - sumPct).toFixed(1);
            if (Math.abs(diffPct) < 0.1) {
                updateStatusBadge(`Percentages total 100%!`, true);
            } else {
                updateStatusBadge(`Total: ${sumPct.toFixed(1)}% (${diffPct > 0 ? diffPct + '% remaining' : Math.abs(diffPct) + '% over'})`, false);
            }
        }
        else if (selectedType === "4" || selectedType === "Shares") {
            let totalShares = 0;
            splitRows.forEach(row => {
                const cb = row.querySelector('.participant-checkbox');
                const shInp = row.querySelector('.shares-input');
                if (cb && cb.checked && shInp) {
                    totalShares += parseFloat(shInp.value) || 1;
                }
            });

            splitRows.forEach(row => {
                const cb = row.querySelector('.participant-checkbox');
                const shInp = row.querySelector('.shares-input');
                const badge = row.querySelector('.calculated-share-badge');
                if (cb && cb.checked && shInp && totalShares > 0) {
                    const sh = parseFloat(shInp.value) || 1;
                    const share = (totalAmount * (sh / totalShares)).toFixed(2);
                    if (badge) badge.innerText = '₹' + share;
                } else if (badge) {
                    badge.innerText = '₹0.00';
                }
            });

            updateStatusBadge(`Total: ${totalShares} shares calculated`, true);
        }
    }

    function updateStatusBadge(text, isValid) {
        const badge = document.getElementById('splitValidationBadge');
        if (!badge) return;
        badge.innerText = text;
        badge.className = isValid 
            ? 'badge bg-success-subtle text-success border border-success-subtle px-3 py-2'
            : 'badge bg-warning-subtle text-warning-emphasis border border-warning-subtle px-3 py-2';
    }

    // Attach listeners
    [baseAmountInput, taxInput, tipInput, serviceInput, discountInput].forEach(inp => {
        if (inp) inp.addEventListener('input', recalculateShares);
    });

    if (splitTypeSelect) {
        splitTypeSelect.addEventListener('change', updateSplitInputsVisibility);
    }

    document.querySelectorAll('.participant-checkbox, .exact-amount-input, .percentage-input, .shares-input').forEach(inp => {
        inp.addEventListener('input', recalculateShares);
    });

    // Select All / Deselect All
    const selectAllBtn = document.getElementById('selectAllParticipants');
    if (selectAllBtn) {
        selectAllBtn.addEventListener('click', function () {
            document.querySelectorAll('.participant-checkbox').forEach(cb => cb.checked = true);
            recalculateShares();
        });
    }

    // Initial Run
    updateSplitInputsVisibility();

    // ----------------- Mock Receipt Scanner / OCR -----------------
    const scanReceiptBtn = document.getElementById('scanReceiptBtn');
    const receiptFileInput = document.getElementById('ReceiptFile');

    if (scanReceiptBtn && receiptFileInput) {
        scanReceiptBtn.addEventListener('click', function () {
            if (!receiptFileInput.files || receiptFileInput.files.length === 0) {
                alert('Please choose an image file first to simulate receipt scanning.');
                receiptFileInput.click();
                return;
            }

            const formData = new FormData();
            formData.append('file', receiptFileInput.files[0]);

            scanReceiptBtn.disabled = true;
            scanReceiptBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Extracting details...';

            fetch('/Expenses/ScanReceiptMock', {
                method: 'POST',
                body: formData
            })
            .then(res => res.json())
            .then(data => {
                scanReceiptBtn.disabled = false;
                scanReceiptBtn.innerHTML = '<i class="bi bi-magic me-1"></i> Scan & Auto-Fill';

                if (data.success) {
                    const titleInp = document.getElementById('Title');
                    if (titleInp && !titleInp.value) titleInp.value = data.merchant;
                    if (baseAmountInput) baseAmountInput.value = data.baseAmount;
                    if (taxInput) taxInput.value = data.taxAmount;
                    const dateInp = document.getElementById('ExpenseDate');
                    if (dateInp) dateInp.value = data.date;

                    const notesInp = document.getElementById('Notes');
                    if (notesInp) notesInp.value = (notesInp.value ? notesInp.value + '\n' : '') + data.notes;

                    recalculateShares();
                    if (typeof showToast === 'function') {
                        showToast(`✨ Extracted bill for "${data.merchant}" (₹${data.baseAmount})!`, 'success');
                    }
                }
            })
            .catch(() => {
                scanReceiptBtn.disabled = false;
                scanReceiptBtn.innerHTML = '<i class="bi bi-magic me-1"></i> Scan & Auto-Fill';
            });
        });
    }
});
